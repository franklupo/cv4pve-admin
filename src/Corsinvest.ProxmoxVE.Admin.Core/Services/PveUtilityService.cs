/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
namespace Corsinvest.ProxmoxVE.Admin.Core.Services;

public class PveUtilityService(IPveClientService pveClientService,
                               ISshService sshService,
                               ILogger<PveUtilityService> logger) : IPveUtilityService
{
    public async Task<FluentResults.Result> BlinkDiskLedAsync(string clusterName, string node, string devPath, bool blink)
    {
        FluentResults.Result result;

        try
        {
            var clusterOptions = (await pveClientService.GetCurrentClusterOptionsAsync())!;
            var (host, ipAddress) = (await GetHostAndIpAsync(clusterOptions)).Where(a => a.Key == node).FirstOrDefault();
            var info = clusterOptions.GetNodeOptions(ipAddress, host);
            if (info != null)
            {
                var ret = sshService.Execute(clusterOptions,
                                              ipAddress,
                                              info.SshPort,
                                              [$"ledctl locate{(blink ? "" : "_off")}={devPath}"])
                                     .ToList();

                result = FluentResults.Result.OkIf(ret[0].ExitCode == 0, ret[0].Error);
            }
            else
            {
                result = FluentResults.Result.Fail("No host found!");
            }
        }
        catch (Exception ex)
        {
            result = FluentResults.Result.Fail(ex.Message);
        }
        return result;
    }

    private async Task<IReadOnlyDictionary<string, string>> GetHostAndIpAsync(ClusterOptions clusterOptions)
    {
        var client = await pveClientService.GetClientAsync(clusterOptions);

        //decode host ip
        return await client.GetHostAndIpAsync();
    }

    public async Task<IEnumerable<FluentResults.Result>> FreeMemoryAsync(string clusterName, IEnumerable<string> nodes)
    {
        var results = new List<FluentResults.Result>();

        var command = @"echo 1 > /proc/sys/vm/compact_memory; echo 3 > /proc/sys/vm/drop_caches;";

        try
        {
            var clusterOptions = (await pveClientService.GetCurrentClusterOptionsAsync())!;
            //decode host ip
            foreach (var (host, ipAddress) in await GetHostAndIpAsync(clusterOptions))
            {
                if (nodes.Contains(host))
                {
                    var info = clusterOptions.GetNodeOptions(ipAddress, host);
                    if (info != null)
                    {
                        var rets = sshService.Execute(clusterOptions, ipAddress, info.SshPort, [command]);
                        foreach (var (ExitCode, _, Error) in rets)
                        {
                            results.Add(FluentResults.Result.OkIf(ExitCode == 0, Error));
                        }
                    }
                    else
                    {
                        results.Add(FluentResults.Result.Fail("No host found!"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            results.Add(FluentResults.Result.Fail(ex.Message));
        }

        return results;
    }
}
