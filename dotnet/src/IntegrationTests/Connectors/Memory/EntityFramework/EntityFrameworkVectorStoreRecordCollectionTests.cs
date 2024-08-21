// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Connectors.EntityFramework;
using Xunit;

namespace SemanticKernel.IntegrationTests.Connectors.Memory.EntityFramework;

public sealed class EntityFrameworkVectorStoreRecordCollectionTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _contextOptions;

    public EntityFrameworkVectorStoreRecordCollectionTests()
    {
        this._connection = new SqliteConnection("Filename=:memory:");
        this._connection.Open();

        this._contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(this._connection)
            .Options;

        using var context = new ApplicationDbContext(this._contextOptions);

        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task ItCanUpsertAndGetAsync()
    {
        // Arrange
        const string HotelId = "55555555-5555-5555-5555-555555555555";

        await using var context = this.CreateContext();

        var sut = new EntityFrameworkVectorStoreRecordCollection<EntityFrameworkHotel>(context);

        var record = this.CreateTestHotel(HotelId);

        // Act
        var upsertResult = await sut.UpsertAsync(record);
        var getResult = await sut.GetAsync(HotelId);

        // Assert
        Assert.Equal(HotelId, upsertResult);
        Assert.NotNull(getResult);

        Assert.Equal(record.HotelId, getResult.HotelId);
        Assert.Equal(record.HotelName, getResult.HotelName);
        Assert.Equal(record.HotelCode, getResult.HotelCode);
        Assert.Equal(record.HotelRating, getResult.HotelRating);
        Assert.Equal(record.ParkingIncluded, getResult.ParkingIncluded);
        Assert.Equal(record.Tags.ToArray(), getResult.Tags.ToArray());
        Assert.Equal(record.Description, getResult.Description);
        Assert.Equal(record.DescriptionEmbedding, getResult.DescriptionEmbedding);
    }

    [Fact]
    public async Task ItCanDeleteAsync()
    {
        // Arrange
        const string HotelId = "55555555-5555-5555-5555-555555555555";

        await using var context = this.CreateContext();

        var sut = new EntityFrameworkVectorStoreRecordCollection<EntityFrameworkHotel>(context);

        var record = this.CreateTestHotel(HotelId);

        // Act
        var upsertResult = await sut.UpsertAsync(record);
        var getResult = await sut.GetAsync(HotelId);

        Assert.Equal(HotelId, upsertResult);
        Assert.NotNull(getResult);

        await sut.DeleteAsync(HotelId);

        getResult = await sut.GetAsync(HotelId);

        Assert.Null(getResult);
    }

    [Fact]
    public async Task ItCanGetAndDeleteBatchAsync()
    {
        // Arrange
        const string HotelId1 = "11111111-1111-1111-1111-111111111111";
        const string HotelId2 = "22222222-2222-2222-2222-222222222222";
        const string HotelId3 = "33333333-3333-3333-3333-333333333333";

        await using var context = this.CreateContext();

        var sut = new EntityFrameworkVectorStoreRecordCollection<EntityFrameworkHotel>(context);

        var record1 = this.CreateTestHotel(HotelId1);
        var record2 = this.CreateTestHotel(HotelId2);
        var record3 = this.CreateTestHotel(HotelId3);

        var upsertResults = await sut.UpsertBatchAsync([record1, record2, record3]).ToListAsync();
        var getResults = await sut.GetBatchAsync([HotelId1, HotelId2, HotelId3]).ToListAsync();

        Assert.Equal([HotelId1, HotelId2, HotelId3], upsertResults);

        Assert.NotNull(getResults.First(l => l.HotelId == HotelId1));
        Assert.NotNull(getResults.First(l => l.HotelId == HotelId2));
        Assert.NotNull(getResults.First(l => l.HotelId == HotelId3));

        // Act
        await sut.DeleteBatchAsync([HotelId1, HotelId2, HotelId3]);

        getResults = await sut.GetBatchAsync([HotelId1, HotelId2, HotelId3]).ToListAsync();

        // Assert
        Assert.Empty(getResults);
    }

    [Fact]
    public async Task ItCanUpsertRecordAsync()
    {
        // Arrange
        const string HotelId = "55555555-5555-5555-5555-555555555555";

        await using var context = this.CreateContext();

        var sut = new EntityFrameworkVectorStoreRecordCollection<EntityFrameworkHotel>(context);

        var record = this.CreateTestHotel(HotelId);

        var upsertResult = await sut.UpsertAsync(record);
        var getResult = await sut.GetAsync(HotelId);

        Assert.Equal(HotelId, upsertResult);
        Assert.NotNull(getResult);

        // Act
        record.HotelName = "Updated name";
        record.HotelRating = 10;

        upsertResult = await sut.UpsertAsync(record);
        getResult = await sut.GetAsync(HotelId);

        // Assert
        Assert.NotNull(getResult);
        Assert.Equal("Updated name", getResult.HotelName);
        Assert.Equal(10, getResult.HotelRating);
    }

    [Fact]
    public async Task ItCanUpsertBatchAsync()
    {
        // Arrange
        const string HotelId1 = "11111111-1111-1111-1111-111111111111";
        const string HotelId2 = "22222222-2222-2222-2222-222222222222";
        const string HotelId3 = "33333333-3333-3333-3333-333333333333";

        await using var context = this.CreateContext();

        var sut = new EntityFrameworkVectorStoreRecordCollection<EntityFrameworkHotel>(context);

        var record1 = this.CreateTestHotel(HotelId1);
        var record2 = this.CreateTestHotel(HotelId2);
        var record3 = this.CreateTestHotel(HotelId3);

        var upsertResults = await sut.UpsertBatchAsync([record1, record2, record3]).ToListAsync();
        var getResults = await sut.GetBatchAsync([HotelId1, HotelId2, HotelId3]).ToListAsync();

        Assert.Equal([HotelId1, HotelId2, HotelId3], upsertResults);

        Assert.NotNull(getResults.First(l => l.HotelId == HotelId1));
        Assert.NotNull(getResults.First(l => l.HotelId == HotelId2));
        Assert.NotNull(getResults.First(l => l.HotelId == HotelId3));

        // Act
        record1.HotelName = "Updated name 1";
        record1.HotelRating = 1;

        record2.HotelName = "Updated name 2";
        record2.HotelRating = 2;

        record3.HotelName = "Updated name 3";
        record3.HotelRating = 3;

        upsertResults = await sut.UpsertBatchAsync([record1, record2, record3]).ToListAsync();
        getResults = await sut.GetBatchAsync([HotelId1, HotelId2, HotelId3]).ToListAsync();

        // Assert
        Assert.NotNull(getResults);

        Assert.Equal("Updated name 1", getResults[0].HotelName);
        Assert.Equal(1, getResults[0].HotelRating);

        Assert.Equal("Updated name 2", getResults[1].HotelName);
        Assert.Equal(2, getResults[1].HotelRating);

        Assert.Equal("Updated name 3", getResults[2].HotelName);
        Assert.Equal(3, getResults[2].HotelRating);
    }

    public void Dispose()
    {
        using var context = this.CreateContext();

        context.Database.EnsureDeleted();

        this._connection.Dispose();
    }

    #region private

    private ApplicationDbContext CreateContext() => new(this._contextOptions);

    private EntityFrameworkHotel CreateTestHotel(string hotelId)
    {
        return new EntityFrameworkHotel
        {
            HotelId = hotelId,
            HotelName = $"My Hotel {hotelId}",
            HotelCode = 42,
            HotelRating = 4.5f,
            ParkingIncluded = true,
            Tags = { "t1", "t2" },
            Description = "This is a great hotel.",
            DescriptionEmbedding = ConvertToByteArray(new[] { 30f, 31f, 32f, 33f }),
        };
    }

    private static byte[] ConvertToByteArray(ReadOnlyMemory<float> memory)
    {
        var length = memory.Length * sizeof(float);
        var bytes = new byte[length];
        MemoryMarshal.AsBytes(memory.Span).CopyTo(bytes);
        return bytes;
    }

    #endregion
}
