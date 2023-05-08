﻿/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using BlazorDownloadFile;
using Corsinvest.ProxmoxVE.Admin.Core.Services;
using Corsinvest.ProxmoxVE.Admin.Diagnostic.Repository;

namespace Corsinvest.ProxmoxVE.Admin.Diagnostic.Components;

public partial class Results
{
    [Parameter] public string Height { get; set; } = default!;

    [Inject] private IPveClientService PveClientService { get; set; } = default!;
    [Inject] private IBlazorDownloadFileService BlazorDownloadFileService { get; set; } = default!;
    [Inject] private IReadRepository<IgnoredIssue> IgnoredIssuesRepo { get; set; } = default!;
    [Inject] private IReadRepository<Execution> ExecutionsRepo { get; set; } = default!;
    [Inject] private IDataGridManagerRepository<Execution> DataGridManager { get; set; } = default!;
    [Inject] private IJobService JobService { get; set; } = default!;
    [Inject] private IOptionsSnapshot<Options> Options { get; set; } = default!;
    [Inject] private IOptionsSnapshot<AppOptions> AppOptions { get; set; } = default!;

    protected override void OnInitialized()
    {
        DataGridManager.Title = L["Results"];
        DataGridManager.DefaultSort = new() { [nameof(Execution.Date)] = true };

        DataGridManager.QueryAsync = async () =>
        {
            var clusterName = await PveClientService.GetCurrentClusterName();
            return await DataGridManager.Repository.ListAsync(new ExecutionSpec(clusterName));
        };
    }

    private async Task Run()
    {
        var clusterName = await PveClientService.GetCurrentClusterName();
        JobService.Schedule<Job>(a => a.Create(clusterName), TimeSpan.FromSeconds(10));
        UINotifier.Show(L["Diagnostic jobs started!"], UINotifierSeverity.Info);
    }

    private async Task DownloadPdf(Execution item)
    {
        var clusterName = await PveClientService.GetCurrentClusterName();
        var ignoreIssues = await Helper.GetIgnoredIssue(IgnoredIssuesRepo, clusterName);
        var data = (await ExecutionsRepo.FirstOrDefaultAsync(new ExecutionSpec(string.Empty).ByKey(item.Id).Include()))!.Data;
        using var ms = Helper.GeneratePdf(L, AppOptions.Value, item, Helper.Analyze(data, Options.Value.Get(clusterName), ignoreIssues))!;
        await BlazorDownloadFileService.DownloadFile("Diagnostic.pdf", ms, System.Net.Mime.MediaTypeNames.Application.Pdf);
    }
}