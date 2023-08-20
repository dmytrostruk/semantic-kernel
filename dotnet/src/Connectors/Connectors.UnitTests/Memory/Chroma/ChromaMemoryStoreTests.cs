// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.Memory.Chroma;
using Microsoft.SemanticKernel.Connectors.Memory.Chroma.Http.ApiSchema;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;
using NSubstitute;
using Xunit;
using NSubstitute.ExceptionExtensions;

namespace SemanticKernel.Connectors.UnitTests.Memory.Chroma;

/// <summary>
/// Unit tests for <see cref="ChromaMemoryStore"/> class.
/// </summary>
public sealed class ChromaMemoryStoreTests : IDisposable
{
    private const string CollectionId = "fake-collection-id";
    private const string CollectionName = "fake-collection-name";

    private readonly HttpMessageHandlerStub _messageHandlerStub;
    private readonly HttpClient _httpClient;
    private readonly IChromaClient _chromaClientMock;
    private readonly JsonSerializerOptions _serializerOptions;

    public ChromaMemoryStoreTests()
    {
        this._messageHandlerStub = new HttpMessageHandlerStub();
        this._httpClient = this.GetHttpClientStub();
        this._chromaClientMock = Substitute.For<IChromaClient>();

        this._chromaClientMock
            .GetCollectionAsync(CollectionName, CancellationToken.None)
            .Returns(new ChromaCollectionModel { Id = CollectionId, Name = CollectionName });

        this._serializerOptions = new JsonSerializerOptions
        {
            Converters = { new ChromaBooleanConverter() }
        };
    }

    [Fact]
    public async Task ItUsesProvidedEndpointFromConstructorAsync()
    {
        // Arrange
        const string endpoint = "https://fake-random-test-host/fake-path/";
        var store = new ChromaMemoryStore(this._httpClient, endpoint);

        // Act
        await store.GetAsync("fake-collection", "fake-key");

        // Assert
        Assert.StartsWith(endpoint, this._messageHandlerStub.RequestUri?.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ItUsesBaseAddressFromHttpClientAsync()
    {
        // Arrange
        const string baseAddress = "https://fake-random-test-host/fake-path/";

        using var httpClient = this.GetHttpClientStub();
        httpClient.BaseAddress = new Uri(baseAddress);

        var store = new ChromaMemoryStore(httpClient);

        // Act
        await store.GetAsync("fake-collection", "fake-key");

        // Assert
        Assert.StartsWith(baseAddress, this._messageHandlerStub.RequestUri?.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ItCanCreateCollectionAsync()
    {
        // Arrange
        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        await store.CreateCollectionAsync(CollectionName);

        // Assert
        await this._chromaClientMock.Received(1).CreateCollectionAsync(CollectionName, CancellationToken.None);
    }

    [Fact]
    public async Task ItCanDeleteCollectionAsync()
    {
        // Arrange
        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        await store.DeleteCollectionAsync(CollectionName);

        // Assert
        await this._chromaClientMock.Received(1).DeleteCollectionAsync(CollectionName, CancellationToken.None);
    }

    [Fact]
    public async Task ItThrowsExceptionOnNonExistentCollectionDeletionAsync()
    {
        // Arrange
        const string collectionName = "non-existent-collection";
        const string collectionDoesNotExistErrorMessage = $"Collection {collectionName} does not exist";
        const string expectedExceptionMessage = $"Cannot delete non-existent collection {collectionName}";

        this._chromaClientMock
            .DeleteCollectionAsync(collectionName, CancellationToken.None)
            .Throws(new SKException(collectionDoesNotExistErrorMessage));

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var exception = await Record.ExceptionAsync(() => store.DeleteCollectionAsync(collectionName));

        // Assert
        Assert.IsType<SKException>(exception);
        Assert.Equal(expectedExceptionMessage, exception.Message);
    }

    [Fact]
    public async Task ItReturnsTrueWhenCollectionExistsAsync()
    {
        // Arrange
        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var doesCollectionExist = await store.DoesCollectionExistAsync(CollectionName);

        // Assert
        Assert.True(doesCollectionExist);
    }

    [Fact]
    public async Task ItReturnsFalseWhenCollectionDoesNotExistAsync()
    {
        // Arrange
        const string collectionName = "non-existent-collection";
        const string collectionDoesNotExistErrorMessage = $"Collection {collectionName} does not exist";

        this._chromaClientMock
            .GetCollectionAsync(collectionName, CancellationToken.None)
            .Throws(new SKException(collectionDoesNotExistErrorMessage));

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var doesCollectionExist = await store.DoesCollectionExistAsync(collectionName);

        // Assert
        Assert.False(doesCollectionExist);
    }

    [Fact]
    public async Task ItCanGetMemoryRecordFromCollectionAsync()
    {
        // Arrange
        var expectedMemoryRecord = this.GetRandomMemoryRecord();
        var embeddingsModel = this.GetEmbeddingsModelFromMemoryRecord(expectedMemoryRecord);

        this._chromaClientMock
            .GetEmbeddingsAsync(CollectionId, Arg.Is<string[]>(x => x.SequenceEqual(new[] { expectedMemoryRecord.Key })), Arg.Any<string[]>(), CancellationToken.None)
            .Returns(embeddingsModel);

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var actualMemoryRecord = await store.GetAsync(CollectionName, expectedMemoryRecord.Key, withEmbedding: true);

        // Assert
        Assert.NotNull(actualMemoryRecord);
        this.AssertMemoryRecordEqual(expectedMemoryRecord, actualMemoryRecord);
    }

    [Fact]
    public async Task ItReturnsNullWhenMemoryRecordDoesNotExistAsync()
    {
        // Arrange
        const string memoryRecordKey = "fake-record-key";

        this._chromaClientMock
            .GetEmbeddingsAsync(CollectionId, Arg.Is<string[]>(x => x.SequenceEqual(new[] { memoryRecordKey })), Arg.Any<string[]>(), CancellationToken.None)
            .Returns(new ChromaEmbeddingsModel());

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var actualMemoryRecord = await store.GetAsync(CollectionName, memoryRecordKey, withEmbedding: true);

        // Assert
        Assert.Null(actualMemoryRecord);
    }

    [Fact]
    public async Task ItThrowsExceptionOnGettingMemoryRecordFromNonExistingCollectionAsync()
    {
        // Arrange
        const string collectionName = "non-existent-collection";
        const string memoryRecordKey = "fake-record-key";
        const string collectionDoesNotExistErrorMessage = $"Collection {collectionName} does not exist";

        this._chromaClientMock
            .GetCollectionAsync(collectionName, CancellationToken.None)
            .Throws(new SKException(collectionDoesNotExistErrorMessage));

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var exception = await Record.ExceptionAsync(() => store.GetAsync(collectionName, memoryRecordKey, withEmbedding: true));

        // Assert
        Assert.IsType<SKException>(exception);
        Assert.Equal(collectionDoesNotExistErrorMessage, exception.Message);
    }

    [Fact]
    public async Task ItCanGetMemoryRecordBatchFromCollectionAsync()
    {
        // Arrange
        var memoryRecord1 = this.GetRandomMemoryRecord();
        var memoryRecord2 = this.GetRandomMemoryRecord();
        var memoryRecord3 = this.GetRandomMemoryRecord();

        var expectedMemoryRecords = new[] { memoryRecord1, memoryRecord2, memoryRecord3 };
        var memoryRecordKeys = expectedMemoryRecords.Select(l => l.Key).ToArray();

        var embeddingsModel = this.GetEmbeddingsModelFromMemoryRecords(expectedMemoryRecords);

        this._chromaClientMock
            .GetEmbeddingsAsync(CollectionId, Arg.Is<string[]>(x => x.SequenceEqual(memoryRecordKeys)), Arg.Any<string[]>(), CancellationToken.None)
            .Returns(embeddingsModel);

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var actualMemoryRecords = await store.GetBatchAsync(CollectionName, memoryRecordKeys, withEmbeddings: true).ToListAsync();

        // Assert
        Assert.Equal(expectedMemoryRecords.Length, actualMemoryRecords.Count);

        for (var i = 0; i < expectedMemoryRecords.Length; i++)
        {
            this.AssertMemoryRecordEqual(expectedMemoryRecords[i], actualMemoryRecords[i]);
        }
    }

    [Fact]
    public async Task ItCanReturnCollectionsAsync()
    {
        // Arrange
        var expectedCollections = new List<string> { "fake-collection-1", "fake-collection-2", "fake-collection-3" };

        this._chromaClientMock
            .ListCollectionsAsync(CancellationToken.None)
            .Returns(expectedCollections.ToAsyncEnumerable());

        var store = new ChromaMemoryStore(this._chromaClientMock);

        // Act
        var actualCollections = await store.GetCollectionsAsync().ToListAsync();

        // Assert
        Assert.Equal(expectedCollections.Count, actualCollections.Count);

        for (var i = 0; i < expectedCollections.Count; i++)
        {
            Assert.Equal(expectedCollections[i], actualCollections[i]);
        }
    }

    public void Dispose()
    {
        this._httpClient.Dispose();
        this._messageHandlerStub.Dispose();
    }

    #region private ================================================================================

    private void AssertMemoryRecordEqual(MemoryRecord expectedRecord, MemoryRecord actualRecord)
    {
        Assert.Equal(expectedRecord.Key, actualRecord.Key);
        Assert.True(expectedRecord.Embedding.Span.SequenceEqual(actualRecord.Embedding.Span));
        Assert.Equal(expectedRecord.Metadata.Id, actualRecord.Metadata.Id);
        Assert.Equal(expectedRecord.Metadata.Text, actualRecord.Metadata.Text);
        Assert.Equal(expectedRecord.Metadata.Description, actualRecord.Metadata.Description);
        Assert.Equal(expectedRecord.Metadata.AdditionalMetadata, actualRecord.Metadata.AdditionalMetadata);
        Assert.Equal(expectedRecord.Metadata.IsReference, actualRecord.Metadata.IsReference);
        Assert.Equal(expectedRecord.Metadata.ExternalSourceName, actualRecord.Metadata.ExternalSourceName);
    }

    private HttpClient GetHttpClientStub()
    {
        return new HttpClient(this._messageHandlerStub, false);
    }

    private MemoryRecord GetRandomMemoryRecord(ReadOnlyMemory<float>? embedding = null)
    {
        var id = Guid.NewGuid().ToString();
        var memoryEmbedding = embedding ?? new[] { 1f, 3f, 5f };

        return MemoryRecord.LocalRecord(
            id: id,
            text: "text-" + Guid.NewGuid().ToString(),
            description: "description-" + Guid.NewGuid().ToString(),
            embedding: memoryEmbedding,
            additionalMetadata: "metadata-" + Guid.NewGuid().ToString(),
            key: id);
    }

    private Dictionary<string, object> GetEmbeddingMetadataFromMemoryRecord(MemoryRecord memoryRecord)
    {
        var serialized = JsonSerializer.Serialize(memoryRecord.Metadata, this._serializerOptions);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(serialized)!;
    }

    private ChromaEmbeddingsModel GetEmbeddingsModelFromMemoryRecords(MemoryRecord[] memoryRecords)
    {
        var embeddingsModel = new ChromaEmbeddingsModel();

        embeddingsModel.Ids.AddRange(memoryRecords.Select(l => l.Key));
        embeddingsModel.Embeddings.AddRange(memoryRecords.Select(l => l.Embedding.ToArray()));
        embeddingsModel.Metadatas.AddRange(memoryRecords.Select(this.GetEmbeddingMetadataFromMemoryRecord));

        return embeddingsModel;
    }

    private ChromaEmbeddingsModel GetEmbeddingsModelFromMemoryRecord(MemoryRecord memoryRecord)
    {
        return this.GetEmbeddingsModelFromMemoryRecords(new[] { memoryRecord });
    }

    #endregion
}
