// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.OpenAI.Services;
using Microsoft.SemanticKernel.Configuration;
using Microsoft.SemanticKernel.Reliability;
using Moq;
using Xunit;

namespace SemanticKernel.UnitTests.Configuration;

/// <summary>
/// Unit tests of <see cref="KernelConfig"/>.
/// </summary>
public class KernelConfigTests
{
    [Fact]
    public void HttpRetryHandlerFactoryIsSet()
    {
        // Arrange
        var retry = new NullHttpRetryHandlerFactory();
        var config = new KernelConfig();

        // Act
        config.SetHttpRetryHandlerFactory(retry);

        // Assert
        Assert.Equal(retry, config.HttpHandlerFactory);
    }

    [Fact]
    public void HttpRetryHandlerFactoryIsSetWithCustomImplementation()
    {
        // Arrange
        var retry = new Mock<IDelegatingHandlerFactory>();
        var config = new KernelConfig();

        // Act
        config.SetHttpRetryHandlerFactory(retry.Object);

        // Assert
        Assert.Equal(retry.Object, config.HttpHandlerFactory);
    }

    [Fact]
    public void HttpRetryHandlerFactoryIsSetToDefaultHttpRetryHandlerFactoryIfNull()
    {
        // Arrange
        var config = new KernelConfig();

        // Act
        config.SetHttpRetryHandlerFactory(null);

        // Assert
        Assert.IsType<DefaultHttpRetryHandlerFactory>(config.HttpHandlerFactory);
    }

    [Fact]
    public void HttpRetryHandlerFactoryIsSetToDefaultHttpRetryHandlerFactoryIfNotSet()
    {
        // Arrange
        var config = new KernelConfig();

        // Act
        // Assert
        Assert.IsType<DefaultHttpRetryHandlerFactory>(config.HttpHandlerFactory);
    }

    [Fact]
    public void ItFailsWhenAddingCompletionBackendsWithSameLabel()
    {
        var target = new KernelConfig();
        target.AddAzureOpenAICompletionBackend("azure", "depl", "https://url", "key");

        var exception = Assert.Throws<KernelException>(() =>
        {
            target.AddAzureOpenAICompletionBackend("azure", "depl2", "https://url", "key");
        });
        Assert.Equal(KernelException.ErrorCodes.InvalidBackendConfiguration, exception.ErrorCode);
    }

    [Fact]
    public void ItFailsWhenAddingEmbeddingsBackendsWithSameLabel()
    {
        var target = new KernelConfig();
        target.AddAzureOpenAIEmbeddingsBackend("azure", "depl", "https://url", "key");

        var exception = Assert.Throws<KernelException>(() =>
        {
            target.AddAzureOpenAIEmbeddingsBackend("azure", "depl2", "https://url", "key");
        });
        Assert.Equal(KernelException.ErrorCodes.InvalidBackendConfiguration, exception.ErrorCode);
    }

    [Fact]
    public void ItFailsWhenAddingDifferentBackendTypeWithSameLabel()
    {
        var target = new KernelConfig();
        target.AddAzureOpenAICompletionBackend("azure", "depl", "https://url", "key");

        var exception = Assert.Throws<KernelException>(() =>
        {
            target.AddAzureOpenAIEmbeddingsBackend("azure", "depl2", "https://url", "key");
        });

        Assert.True(target.HasBackend("azure"));
        Assert.Equal(KernelException.ErrorCodes.InvalidBackendConfiguration, exception.ErrorCode);
    }

    [Fact]
    public void ItFailsWhenSetNonExistentBackend()
    {
        var target = new KernelConfig();
        var exception = Assert.Throws<KernelException>(() =>
        {
            target.SetDefaultBackend("azure");
        });
        Assert.Equal(KernelException.ErrorCodes.BackendNotFound, exception.ErrorCode);
    }

    [Fact]
    public void ItTellsIfABackendIsAvailable()
    {
        // Arrange
        var target = new KernelConfig();
        target.AddAzureOpenAICompletionBackend("azure_completion", "depl", "https://url", "key");
        target.AddOpenAICompletionBackend("oai_completion", "model", "apikey");
        target.AddAzureOpenAIEmbeddingsBackend("azure_embeddings", "depl2", "https://url2", "key");
        target.AddOpenAIEmbeddingsBackend("oai2_embeddings", "model2", "apikey2");

        // Assert
        Assert.True(target.HasBackend("azure_completion"));
        Assert.True(target.HasBackend("oai_completion"));
        Assert.True(target.HasBackend("azure_embeddings"));
        Assert.True(target.HasBackend("oai2_embeddings"));

        Assert.False(target.HasBackend("azure2_completion"));
        Assert.False(target.HasBackend("oai2_completion"));
        Assert.False(target.HasBackend("azure1_embeddings"));
        Assert.False(target.HasBackend("oai_embeddings"));

        Assert.True(target.HasBackend("azure_completion",
            x => x is AzureTextCompletion));
        Assert.False(target.HasBackend("azure_completion",
            x => x is OpenAITextCompletion));

        Assert.False(target.HasBackend("oai2_embeddings",
            x => x is AzureTextEmbeddings));
        Assert.True(target.HasBackend("oai2_embeddings",
            x => x is OpenAITextEmbeddings));
    }

    [Fact]
    public void ItCanOverwriteBackends()
    {
        // Arrange
        var target = new KernelConfig();

        // Act - Assert no exception occurs
        target.AddAzureOpenAICompletionBackend("one", "dep", "https://localhost", "key", overwrite: true);
        target.AddAzureOpenAICompletionBackend("one", "dep", "https://localhost", "key", overwrite: true);
        target.AddOpenAICompletionBackend("one", "model", "key", overwrite: true);
        target.AddOpenAICompletionBackend("one", "model", "key", overwrite: true);
        target.AddAzureOpenAIEmbeddingsBackend("one", "dep", "https://localhost", "key", overwrite: true);
        target.AddAzureOpenAIEmbeddingsBackend("one", "dep", "https://localhost", "key", overwrite: true);
        target.AddOpenAIEmbeddingsBackend("one", "model", "key", overwrite: true);
        target.AddOpenAIEmbeddingsBackend("one", "model", "key", overwrite: true);
    }

    [Fact]
    public void ItCanRemoveAllBackends()
    {
        // Arrange
        var target = new KernelConfig();
        target.AddAzureOpenAICompletionBackend("one", "dep", "https://localhost", "key");
        target.AddAzureOpenAICompletionBackend("2", "dep", "https://localhost", "key");
        target.AddOpenAICompletionBackend("3", "model", "key");
        target.AddOpenAICompletionBackend("4", "model", "key");
        target.AddAzureOpenAIEmbeddingsBackend("5", "dep", "https://localhost", "key");
        target.AddAzureOpenAIEmbeddingsBackend("6", "dep", "https://localhost", "key");
        target.AddOpenAIEmbeddingsBackend("7", "model", "key");
        target.AddOpenAIEmbeddingsBackend("8", "model", "key");

        // Act
        target.RemoveAllBackends();

        // Assert
        Assert.Empty(target.GetAllBackends());
    }

    [Fact]
    public void ItCanRemoveOneBackend()
    {
        // Arrange
        var target = new KernelConfig();
        target.AddAzureOpenAICompletionBackend("1", "dep", "https://localhost", "key");
        target.AddAzureOpenAICompletionBackend("2", "dep", "https://localhost", "key");
        target.AddOpenAICompletionBackend("3", "model", "key");
        Assert.Equal("1", target.DefaultBackend);

        // Act - Assert
        target.RemoveBackend("1");
        Assert.Equal("2", target.DefaultBackend);
        target.RemoveBackend("2");
        Assert.Equal("3", target.DefaultBackend);
        target.RemoveBackend("3");
        Assert.Null(target.DefaultBackend);
    }

    [Fact]
    public void GetBackendItReturnsDefaultWhenNonExistingLabelIsProvided()
    {
        // Arrange
        var target = new KernelConfig();
        var defaultBackendLabel = "2";
        target.AddAzureOpenAICompletionBackend("1", "dep", "https://localhost", "key");
        target.AddAzureOpenAIEmbeddingsBackend(defaultBackendLabel, "dep", "https://localhost", "key");
        target.SetDefaultBackend(defaultBackendLabel);

        // Act
        var result = target.GetBackend("test");

        // Assert
        Assert.IsType<AzureTextEmbeddings>(result);
    }

    [Fact]
    public void GetEmbeddingsBackendItReturnsSpecificWhenExistingLabelIsProvided()
    {
        // Arrange
        var target = new KernelConfig();
        var specificBackendLabel = "1";
        var defaultBackendLabel = "2";
        target.AddOpenAIEmbeddingsBackend(specificBackendLabel, "dep", "https://localhost", "key");
        target.AddAzureOpenAIEmbeddingsBackend(defaultBackendLabel, "dep", "https://localhost", "key");
        target.SetDefaultBackend(defaultBackendLabel);

        // Act
        var result = target.GetBackend(specificBackendLabel);

        // Assert
        Assert.IsType<OpenAITextEmbeddings>(result);
    }

    [Fact]
    public void GetEmbeddingsBackendItReturnsDefaultWhenNoLabelIsProvided()
    {
        // Arrange
        var target = new KernelConfig();
        var defaultBackendLabel = "2";
        target.AddAzureOpenAICompletionBackend("1", "dep", "https://localhost", "key");
        target.AddAzureOpenAIEmbeddingsBackend(defaultBackendLabel, "dep", "https://localhost", "key");
        target.SetDefaultBackend(defaultBackendLabel);

        // Act
        var result = target.GetBackend();

        // Assert
        Assert.IsType<AzureTextEmbeddings>(result);
    }
}
