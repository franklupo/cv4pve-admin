/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Microsoft.EntityFrameworkCore;

namespace Corsinvest.ProxmoxVE.Admin.NodeProtect.Persistence;

internal class NodeProtectDbContext : DbContext
{
    public NodeProtectDbContext(DbContextOptions<NodeProtectDbContext> options) : base(options) { }

    public DbSet<NodeProtectJobHistory> JobHistories { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseCollation("NOCASE");
    }
}