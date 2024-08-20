// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;

namespace SemanticKernel.IntegrationTests.Connectors.Memory.EntityFramework;

public sealed class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<EntityFrameworkHotel> Hotels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityFrameworkHotel>()
            .HasKey(l => l.HotelId);
    }
}
