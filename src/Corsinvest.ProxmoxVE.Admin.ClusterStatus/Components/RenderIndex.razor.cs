﻿/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Corsinvest.AppHero.Core.Domain.Contracts;
using Corsinvest.ProxmoxVE.Admin.Core.Helpers;
using Corsinvest.ProxmoxVE.Admin.Core.Models;
using Corsinvest.ProxmoxVE.Admin.Core.Services.DiskInfo;
using Corsinvest.ProxmoxVE.Admin.Core.UI.ProxmoxVE.Cluster;
using Corsinvest.ProxmoxVE.Admin.Core.UI.ProxmoxVE.Common;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Mapster;
using ClasterStatusModel = Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster.ClusterStatus;

namespace Corsinvest.ProxmoxVE.Admin.ClusterStatus.Components;

public partial class RenderIndex : IRefreshable
{
    [Inject] private IPveClientService PveClientService { get; set; } = default!;

    private PveClient PveClient { get; set; } = default!;
    private Core.Configurations.ClusterOptions ClusterOptions { get; set; } = default!;
    private Summary? RefSummary { get; set; } = default!;
    private Resources<ClusterResourceVmExtraInfo>? RefResources { get; set; } = default!;
    private Tasks? RefTasks { get; set; } = default!;
    private Logs? RefLogs { get; set; } = default!;

    public async Task Refresh()
    {
        await RefSummary!.Refresh();
        await RefResources!.Refresh();

        if (RefTasks != null) { await RefTasks.Refresh(); }
        if (RefLogs != null) { await RefLogs.Refresh(); }
    }

    private IEnumerable<string> ResourcesColumns
    {
        get
        {
            var data = new List<string>()
            {
                nameof(ClusterResource.Status),
                nameof(ClusterResource.Type),
                nameof(ClusterResource.Description),
                nameof(ClusterResource.DiskUsagePercentage),
                nameof(ClusterResource.MemoryUsagePercentage),
                nameof(ClusterResource.CpuUsagePercentage),
                nameof(ClusterResource.HostCpuUsage),
                nameof(ClusterResource.HostMemoryUsage),
            };

            if (ClusterOptions.CalculateSnapshotSize)
            {
                data.Add(nameof(ClusterResourceVmExtraInfo.SnapshotsSize));
            }

            //nameof(ClusterResourceVmExtraInfo.HostName),
            //nameof(ClusterResourceVmExtraInfo.OsVersion),
            //nameof(ClusterResourceVmExtraInfo.OsType),
            data.Add(nameof(ClusterResource.IsLocked));
            data.Add(nameof(ClusterResource.Tags));
            data.Add(nameof(ClusterResource.Uptime));

            return data;
        }
    }

    private IEnumerable<DiskInfoBase> Disks { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            PveClient = await PveClientService.GetClientCurrentClusterAsync();
            ClusterOptions = (await PveClientService.GetCurrentClusterOptionsAsync())!;
        }
        catch { }
    }

    private async Task<IEnumerable<ClusterResource>> GetResourcesBase()
        => (await PveClient.GetResources(ClusterResourceType.All)).CalculateHostUsage();


    private async Task<IEnumerable<ClusterResourceVmExtraInfo>> GetResources()
    {
        var data = (await GetResourcesBase())
                    .AsQueryable()
                    .ProjectToType<ClusterResourceVmExtraInfo>()
                    .ToArray();

        if (ClusterOptions.CalculateSnapshotSize)
        {
            //snapshot size
            Disks = await PveAdminHelper.MapSnapshotSize(PveClient, PveClientService, data, false, false);
            StateHasChanged();
        }

        //await VmHelper.PopulateVmOsInfo(PveClient, data.Where(a => a is IClusterResourceVm));

        return data;
    }

    private async Task<IEnumerable<ClasterStatusModel>> GetStatus() => await PveClient.Cluster.Status.Get();
    private async Task<string?> GetCephStatus()
    {
        var result = await PveClient.Cluster.Ceph.Status.Status();
        return result.IsSuccessStatusCode
                ? result.Response.data.health.status
                : null;
    }

    private async Task<IEnumerable<NodeTask>> GetTasks() => await PveClient.Cluster.Tasks.Get();
}