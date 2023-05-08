﻿/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Corsinvest.AppHero.Core.Options;
using Corsinvest.AppHero.Core.UI;
using MudExtensions;
using MudExtensions.Enums;

namespace Corsinvest.ProxmoxVE.Admin.Core.UI.Dialogs;

public partial class DialogFastConfig
{
    [Inject] private IWritableOptionsService<AdminOptions> WritableOptionsService { get; set; } = default!;
    [Inject] private IOptionsSnapshot<AdminOptions> AdminOptions { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private MudStepper? RefStepper { get; set; } = default!;
    private MudForm? RefForm { get; set; } = default!;

    public ClusterOptions ClusterOptions => AdminOptions.Value.Clusters[0];

    protected override void OnInitialized()
    {
        if (!AdminOptions.Value.Clusters.Any()) { AdminOptions.Value.Clusters.Add(new()); }
    }

    private async Task<bool> PreventStepChangeAsync(StepChangeDirection direction)
    {
        var ret = false;

        if (direction == StepChangeDirection.Forward)
        {
            switch (RefStepper!.GetActiveIndex())
            {
                case 0:
                    ret = string.IsNullOrEmpty(ClusterOptions.ApiToken)
                            && (string.IsNullOrEmpty(ClusterOptions.ApiCredential.Username)
                                || string.IsNullOrEmpty(ClusterOptions.ApiCredential.Username));

                    if (ret) { UINotifier.Show("ApiToken or credential is required!", UINotifierSeverity.Error); }
                    break;

                case 1:
                    ret = string.IsNullOrEmpty(ClusterOptions.SshCredential.Username)
                            || string.IsNullOrEmpty(ClusterOptions.SshCredential.Username);
                    if (ret) { UINotifier.Show("Credential is required!", UINotifierSeverity.Error); }
                    break;

                case 2:
                    try
                    {
                        //check pve login
                        var client = await PveAdminHelper.GetPveClient(ClusterOptions, Logger);
                        ret = client == null;

                        //get cluster name
                        if (client != null) { ClusterOptions.Name = await client.GetClusterName(); }

                        UINotifier.Show(!ret,
                                        "Successful connection!",
                                        "Problem connection!",
                                        "Test connection  Proxmox VE cluster!");

                    }
                    catch (Exception ex)
                    {
                        UINotifier.Show(ex.Message, UINotifierSeverity.Error);
                        ret = true;
                    }
                    break;

                case 3:
                    await RefForm!.Validate();
                    ret = !RefForm.IsValid;

                    break;

                default: break;
            }
        }

        WritableOptionsService.Update(AdminOptions.Value);

        return ret;
    }

    private void ResultStepClick() => NavigationManager.NavigateTo(NavigationManager.Uri, true);
}