﻿/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Corsinvest.ProxmoxVE.Admin.Core.Repository;

namespace Corsinvest.ProxmoxVE.Admin.Core.Extensions;

public static class EnumerableExtension
{
    public static T? IsCluster<T>(this IEnumerable<T> query, string clusterName) where T : IClusterName
        => query.FirstOrDefault(a => a.ClusterName == clusterName);
}