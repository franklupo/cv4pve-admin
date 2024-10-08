/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Corsinvest.ProxmoxVE.Admin.Core.Services;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;

namespace Corsinvest.ProxmoxVE.Admin.ReplicationTrend.Components;

public partial class ClusterReplications
{
    [Parameter] public string Height { get; set; } = default!;

    [Inject] private IDataGridManager<NodeReplication> DataGridManager { get; set; } = default!;
    [Inject] private IPveClientService PveClientService { get; set; } = default!;

    protected override void OnInitialized()
    {
        DataGridManager.Title = L["Replication Trend"];
        DataGridManager.QueryAsync = async () =>
        {
            var client = await PveClientService.GetClientCurrentClusterAsync();
            var ret = new List<NodeReplication>();

            foreach (var node in (await client.GetNodesAsync()).Where(a => a.IsOnline))
            {
                foreach (var job in await client.Nodes[node.Node].Replication.GetAsync())
                {
                    ret.Add(job);
                }
            }

            return ret.OrderBy(a => a.Target).ThenBy(a => a.Guest);
        };
    }
}
