﻿/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Corsinvest.AppHero.Core.Extensions;
using Corsinvest.ProxmoxVE.Admin.Core.UI.ProxmoxVE.Cluster;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Admin.NodeFreeMemory;

public partial class RenderIndex
{
    [Inject] private IPveClientService PveClientService { get; set; } = default!;
    [Inject] private IPveUtilityService PveUtilityService { get; set; } = default!;

    private Resources? RefResources { get; set; }
    private bool LoadingFreeMemory { get; set; }

    private async Task<IEnumerable<ClusterResource>> GetItems()
        => (await (await PveClientService.GetClientCurrentCluster())
            .GetResources(ClusterResourceType.Node))
            .Where(a => a.IsOnline);

    private async Task FreeMemory()
    {
        LoadingFreeMemory = true;
        if (await UIMessageBox.ShowQuestionAsync(L["Unlock"], L["Unlock VM/CT?"]))
        {
            var ret = await PveUtilityService.FreeMemory(await PveClientService.GetCurrentClusterName(),
                                                         RefResources!.DataGridManager.SelectedItems.Select(a => a.Node));

            UINotifier.Show(!ret.Any(a => a.IsFailed),
                            L["Free memory nodes successfully!"],
                            L["Error execution!<br>"] + ret.SelectMany(a => a.Errors.Select(a => a.Message)).JoinAsString("<br>"));

            RefResources!.DataGridManager.SelectedItems.Clear();
            await RefResources!.DataGridManager.Refresh();
        }
        LoadingFreeMemory = false;
    }
}