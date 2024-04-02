﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using Moq;
using Xunit;

namespace SemanticKernel.UnitTests.Filters;

public class KernelFilterTests
{
    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PreInvocationFunctionFilterIsTriggeredAsync(FilterType filterType)
    {
        // Arrange
        var functionInvocations = 0;
        var filterInvocations = 0;
        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var kernel = this.GetKernelWithFilters(filterType,
            onFunctionInvoking: (context) =>
            {
                filterInvocations++;
            });

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(1, functionInvocations);
        Assert.Equal(1, filterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PreInvocationFunctionFilterChangesArgumentAsync(FilterType filterType)
    {
        // Arrange
        const string OriginalInput = "OriginalInput";
        const string NewInput = "NewInput";

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoking: (context) =>
            {
                context.Arguments["originalInput"] = NewInput;
            });

        var function = KernelFunctionFactory.CreateFromMethod((string originalInput) => originalInput);

        // Act
        var result = await kernel.InvokeAsync(function, new() { ["originalInput"] = OriginalInput });

        // Assert
        Assert.Equal(NewInput, result.GetValue<string>());
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PreInvocationFunctionFilterCancellationWorksCorrectlyAsync(FilterType filterType)
    {
        // Arrange
        var functionInvocations = 0;
        var preFilterInvocations = 0;
        var postFilterInvocations = 0;

        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoking: (context) =>
            {
                preFilterInvocations++;
                context.Cancel = true;
            },
            onFunctionInvoked: (context) =>
            {
                Assert.NotNull(context.Exception);
                Assert.IsType<KernelFunctionCanceledException>(context.Exception);

                postFilterInvocations++;
            });

        // Act
        var exception = await Assert.ThrowsAsync<KernelFunctionCanceledException>(() => kernel.InvokeAsync(function));

        // Assert
        Assert.Equal(1, preFilterInvocations);
        Assert.Equal(0, functionInvocations);
        Assert.Equal(1, postFilterInvocations);
        Assert.Same(function, exception.Function);
        Assert.NotNull(exception.FunctionResult);

        var resultValue = exception.FunctionResult.GetValue<object>();
        Assert.Null(resultValue);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PreInvocationFunctionFilterCancellationWorksCorrectlyOnStreamingAsync(FilterType filterType)
    {
        // Arrange
        var functionInvocations = 0;
        var filterInvocations = 0;
        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoking: (context) =>
            {
                filterInvocations++;
                context.Cancel = true;
            },
            onFunctionInvoked: (context) =>
            {
                Assert.NotNull(context.Exception);
                Assert.IsType<KernelFunctionCanceledException>(context.Exception);
            });

        // Act
        IAsyncEnumerable<StreamingKernelContent> enumerable = function.InvokeStreamingAsync<StreamingKernelContent>(kernel);
        IAsyncEnumerator<StreamingKernelContent> enumerator = enumerable.GetAsyncEnumerator();

        Assert.Equal(0, filterInvocations);

        var exception = await Assert.ThrowsAsync<KernelFunctionCanceledException>(async () => await enumerator.MoveNextAsync());

        // Assert
        Assert.Equal(1, filterInvocations);
        Assert.Equal(0, functionInvocations);
        Assert.Same(function, exception.Function);
        Assert.Same(kernel, exception.Kernel);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PostInvocationFunctionFilterIsTriggeredAsync(FilterType filterType)
    {
        // Arrange
        var functionInvocations = 0;
        var filterInvocations = 0;
        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                filterInvocations++;
            });

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(1, functionInvocations);
        Assert.Equal(1, filterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PostInvocationFunctionFilterReturnsModifiedResultAsync(FilterType filterType)
    {
        // Arrange
        const int OriginalResult = 42;
        const int NewResult = 84;

        var function = KernelFunctionFactory.CreateFromMethod(() => OriginalResult);

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                context.SetResultValue(NewResult);
            });

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(NewResult, result.GetValue<int>());
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFiltersAreTriggeredOnStreamingAsync(FilterType filterType)
    {
        // Arrange
        var functionInvocations = 0;
        var preFilterInvocations = 0;
        var postFilterInvocations = 0;

        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoking: (context) =>
            {
                preFilterInvocations++;
            },
            onFunctionInvoked: (context) =>
            {
                postFilterInvocations++;
            });

        // Act
        await foreach (var chunk in kernel.InvokeStreamingAsync(function))
        {
        }

        // Assert
        Assert.Equal(1, functionInvocations);
        Assert.Equal(1, preFilterInvocations);
        Assert.Equal(1, postFilterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFiltersWithPromptsWorkCorrectlyAsync(FilterType filterType)
    {
        // Arrange
        var preFilterInvocations = 0;
        var postFilterInvocations = 0;
        var mockTextGeneration = this.GetMockTextGeneration();

        var kernel = this.GetKernelWithFilters(
            filterType,
            textGenerationService: mockTextGeneration.Object,
            onFunctionInvoking: (context) =>
            {
                preFilterInvocations++;
            },
            onFunctionInvoked: (context) =>
            {
                postFilterInvocations++;
            });

        var function = KernelFunctionFactory.CreateFromPrompt("Write a simple phrase about UnitTests");

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(1, preFilterInvocations);
        Assert.Equal(1, postFilterInvocations);
        mockTextGeneration.Verify(m => m.GetTextContentsAsync(It.IsAny<string>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PromptFiltersAreNotTriggeredForMethodsAsync(FilterType filterType)
    {
        // Arrange
        var functionInvocations = 0;
        var preFilterInvocations = 0;
        var postFilterInvocations = 0;

        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var kernel = this.GetKernelWithFilters(
            filterType,
            onPromptRendering: (context) =>
            {
                preFilterInvocations++;
            },
            onPromptRendered: (context) =>
            {
                postFilterInvocations++;
            });

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(1, functionInvocations);
        Assert.Equal(0, preFilterInvocations);
        Assert.Equal(0, postFilterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PromptFiltersAreTriggeredForPromptsAsync(FilterType filterType)
    {
        // Arrange
        var preFilterInvocations = 0;
        var postFilterInvocations = 0;
        var mockTextGeneration = this.GetMockTextGeneration();

        var function = KernelFunctionFactory.CreateFromPrompt("Prompt");

        var kernel = this.GetKernelWithFilters(
            filterType,
            textGenerationService: mockTextGeneration.Object,
            onPromptRendering: (context) =>
            {
                preFilterInvocations++;
            },
            onPromptRendered: (context) =>
            {
                postFilterInvocations++;
            });

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(1, preFilterInvocations);
        Assert.Equal(1, postFilterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PromptFiltersAreTriggeredForPromptsStreamingAsync(FilterType filterType)
    {
        // Arrange
        var preFilterInvocations = 0;
        var postFilterInvocations = 0;
        var mockTextGeneration = this.GetMockTextGeneration();

        var function = KernelFunctionFactory.CreateFromPrompt("Prompt");

        var kernel = this.GetKernelWithFilters(
            filterType,
            textGenerationService: mockTextGeneration.Object,
            onPromptRendering: (context) =>
            {
                preFilterInvocations++;
            },
            onPromptRendered: (context) =>
            {
                postFilterInvocations++;
            });

        // Act
        await foreach (var chunk in kernel.InvokeStreamingAsync(function))
        {
        }

        // Assert
        Assert.Equal(1, preFilterInvocations);
        Assert.Equal(1, postFilterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task PostInvocationPromptFilterChangesRenderedPromptAsync(FilterType filterType)
    {
        // Arrange
        var mockTextGeneration = this.GetMockTextGeneration();
        var function = KernelFunctionFactory.CreateFromPrompt("Prompt");
        var kernel = this.GetKernelWithFilters(
            filterType,
            textGenerationService: mockTextGeneration.Object,
            onPromptRendered: (context) =>
            {
                context.RenderedPrompt += " - updated from filter";
            });

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        mockTextGeneration.Verify(m => m.GetTextContentsAsync("Prompt - updated from filter", It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task FunctionAndPromptFiltersAreExecutedInCorrectOrderAsync()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mockTextGeneration = this.GetMockTextGeneration();
        var function = KernelFunctionFactory.CreateFromPrompt("Prompt");

        var executionOrder = new List<string>();

        var functionFilter1 = new FakeFunctionFilter(
            (context) => executionOrder.Add("FunctionFilter1-Invoking"),
            (context) => executionOrder.Add("FunctionFilter1-Invoked"));

        var functionFilter2 = new FakeFunctionFilter(
            (context) => executionOrder.Add("FunctionFilter2-Invoking"),
            (context) => executionOrder.Add("FunctionFilter2-Invoked"));

        var promptFilter1 = new FakePromptFilter(
            (context) => executionOrder.Add("PromptFilter1-Rendering"),
            (context) => executionOrder.Add("PromptFilter1-Rendered"));

        var promptFilter2 = new FakePromptFilter(
            (context) => executionOrder.Add("PromptFilter2-Rendering"),
            (context) => executionOrder.Add("PromptFilter2-Rendered"));

        builder.Services.AddSingleton<IFunctionFilter>(functionFilter1);
        builder.Services.AddSingleton<IFunctionFilter>(functionFilter2);

        builder.Services.AddSingleton<IPromptFilter>(promptFilter1);
        builder.Services.AddSingleton<IPromptFilter>(promptFilter2);

        builder.Services.AddSingleton<ITextGenerationService>(mockTextGeneration.Object);

        var kernel = builder.Build();

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal("FunctionFilter1-Invoking", executionOrder[0]);
        Assert.Equal("FunctionFilter2-Invoking", executionOrder[1]);
        Assert.Equal("PromptFilter1-Rendering", executionOrder[2]);
        Assert.Equal("PromptFilter2-Rendering", executionOrder[3]);
        Assert.Equal("PromptFilter1-Rendered", executionOrder[4]);
        Assert.Equal("PromptFilter2-Rendered", executionOrder[5]);
        Assert.Equal("FunctionFilter1-Invoked", executionOrder[6]);
        Assert.Equal("FunctionFilter2-Invoked", executionOrder[7]);
    }

    [Fact]
    public async Task MultipleFunctionFiltersCancellationWorksCorrectlyAsync()
    {
        // Arrange
        var functionInvocations = 0;
        var filterInvocations = 0;
        var function = KernelFunctionFactory.CreateFromMethod(() => functionInvocations++);

        var functionFilter1 = new FakeFunctionFilter(onFunctionInvoking: (context) =>
        {
            filterInvocations++;
            context.Cancel = true;
        });

        var functionFilter2 = new FakeFunctionFilter(onFunctionInvoking: (context) =>
        {
            Assert.True(context.Cancel);

            filterInvocations++;
            context.Cancel = false;
        });

        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton<IFunctionFilter>(functionFilter1);
        builder.Services.AddSingleton<IFunctionFilter>(functionFilter2);

        var kernel = builder.Build();

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal(1, functionInvocations);
        Assert.Equal(2, filterInvocations);
    }

    [Fact]
    public async Task DifferentWaysOfAddingFunctionFiltersWorkCorrectlyAsync()
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => "Result");
        var executionOrder = new List<string>();

        var functionFilter1 = new FakeFunctionFilter((context) => executionOrder.Add("FunctionFilter1-Invoking"));
        var functionFilter2 = new FakeFunctionFilter((context) => executionOrder.Add("FunctionFilter2-Invoking"));

        var builder = Kernel.CreateBuilder();

        // Act

        // Case #1 - Add filter to services
        builder.Services.AddSingleton<IFunctionFilter>(functionFilter1);

        var kernel = builder.Build();

        // Case #2 - Add filter to kernel
        kernel.FunctionFilters.Add(functionFilter2);

        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal("FunctionFilter1-Invoking", executionOrder[0]);
        Assert.Equal("FunctionFilter2-Invoking", executionOrder[1]);
    }

    [Fact]
    public async Task DifferentWaysOfAddingPromptFiltersWorkCorrectlyAsync()
    {
        // Arrange
        var mockTextGeneration = this.GetMockTextGeneration();
        var function = KernelFunctionFactory.CreateFromPrompt("Prompt");
        var executionOrder = new List<string>();

        var promptFilter1 = new FakePromptFilter((context) => executionOrder.Add("PromptFilter1-Rendering"));
        var promptFilter2 = new FakePromptFilter((context) => executionOrder.Add("PromptFilter2-Rendering"));

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ITextGenerationService>(mockTextGeneration.Object);

        // Act
        // Case #1 - Add filter to services
        builder.Services.AddSingleton<IPromptFilter>(promptFilter1);

        var kernel = builder.Build();

        // Case #2 - Add filter to kernel
        kernel.PromptFilters.Add(promptFilter2);

        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal("PromptFilter1-Rendering", executionOrder[0]);
        Assert.Equal("PromptFilter2-Rendering", executionOrder[1]);
    }

    [Fact]
    public async Task InsertFilterInMiddleOfPipelineTriggersFiltersInCorrectOrderAsync()
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => "Result");
        var executionOrder = new List<string>();

        var functionFilter1 = new FakeFunctionFilter(
            (context) => executionOrder.Add("FunctionFilter1-Invoking"),
            (context) => executionOrder.Add("FunctionFilter1-Invoked"));

        var functionFilter2 = new FakeFunctionFilter(
            (context) => executionOrder.Add("FunctionFilter2-Invoking"),
            (context) => executionOrder.Add("FunctionFilter2-Invoked"));

        var functionFilter3 = new FakeFunctionFilter(
            (context) => executionOrder.Add("FunctionFilter3-Invoking"),
            (context) => executionOrder.Add("FunctionFilter3-Invoked"));

        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton<IFunctionFilter>(functionFilter1);
        builder.Services.AddSingleton<IFunctionFilter>(functionFilter2);

        var kernel = builder.Build();

        kernel.FunctionFilters.Insert(1, functionFilter3);

        // Act
        var result = await kernel.InvokeAsync(function);

        // Assert
        Assert.Equal("FunctionFilter1-Invoking", executionOrder[0]);
        Assert.Equal("FunctionFilter3-Invoking", executionOrder[1]);
        Assert.Equal("FunctionFilter2-Invoking", executionOrder[2]);
        Assert.Equal("FunctionFilter1-Invoked", executionOrder[3]);
        Assert.Equal("FunctionFilter3-Invoked", executionOrder[4]);
        Assert.Equal("FunctionFilter2-Invoked", executionOrder[5]);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFilterReceivesInvocationExceptionAsync(FilterType filterType)
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => { throw new NotImplementedException(); });

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                Assert.NotNull(context.Exception);
                Assert.IsType<NotImplementedException>(context.Exception);
            });

        // Act
        var exception = await Assert.ThrowsAsync<NotImplementedException>(() => kernel.InvokeAsync(function));

        // Assert
        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFilterReceivesInvocationExceptionOnStreamingAsync(FilterType filterType)
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => { throw new NotImplementedException(); });

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                Assert.NotNull(context.Exception);
                Assert.IsType<NotImplementedException>(context.Exception);
            });

        // Act
        IAsyncEnumerable<StreamingKernelContent> enumerable = function.InvokeStreamingAsync<StreamingKernelContent>(kernel);
        IAsyncEnumerator<StreamingKernelContent> enumerator = enumerable.GetAsyncEnumerator();

        var exception = await Assert.ThrowsAsync<NotImplementedException>(async () => await enumerator.MoveNextAsync());

        // Assert
        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFilterCanCancelExceptionAsync(FilterType filterType)
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => { throw new NotImplementedException(); });

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                if (context.Exception is not null)
                {
                    context.CancelException();
                }

                context.SetResultValue("Result ignoring exception.");
            });

        // Act
        var result = await kernel.InvokeAsync(function);
        var resultValue = result.GetValue<string>();

        // Assert
        Assert.Equal("Result ignoring exception.", resultValue);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFilterCanCancelExceptionOnStreamingAsync(FilterType filterType)
    {
        // Arrange
        var postFilterInvocations = 0;
        var function = KernelFunctionFactory.CreateFromMethod(() => { throw new NotImplementedException(); });

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                context.CancelException();

                postFilterInvocations++;
            });

        // Act
        await foreach (var chunk in kernel.InvokeStreamingAsync(function))
        {
        }

        // Assert
        Assert.Equal(1, postFilterInvocations);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFilterCanRethrowAnotherTypeOfExceptionAsync(FilterType filterType)
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => { throw new NotImplementedException(); });

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                throw new InvalidOperationException("Exception from filter");
            });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => kernel.InvokeAsync(function));

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("Exception from filter", exception.Message);
    }

    [Theory]
    [InlineData(FilterType.Sync)]
    [InlineData(FilterType.Async)]
    public async Task FunctionFilterCanRethrowAnotherTypeOfExceptionOnStreamingAsync(FilterType filterType)
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromMethod(() => { throw new NotImplementedException(); });

        var kernel = this.GetKernelWithFilters(
            filterType,
            onFunctionInvoked: (context) =>
            {
                throw new InvalidOperationException("Exception from filter");
            });

        // Act
        IAsyncEnumerable<StreamingKernelContent> enumerable = function.InvokeStreamingAsync<StreamingKernelContent>(kernel);
        IAsyncEnumerator<StreamingKernelContent> enumerator = enumerable.GetAsyncEnumerator();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await enumerator.MoveNextAsync());

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("Exception from filter", exception.Message);
    }

    private Kernel GetKernelWithFilters(
        FilterType filterType,
        Action<FunctionInvokingContext>? onFunctionInvoking = null,
        Action<FunctionInvokedContext>? onFunctionInvoked = null,
        Action<PromptRenderingContext>? onPromptRendering = null,
        Action<PromptRenderedContext>? onPromptRendered = null,
        ITextGenerationService? textGenerationService = null)
    {
        return filterType switch
        {
            FilterType.Sync => this.GetKernelWithSyncFilters(onFunctionInvoking, onFunctionInvoked, onPromptRendering, onPromptRendered, textGenerationService),
            FilterType.Async => this.GetKernelWithAsyncFilters(onFunctionInvoking, onFunctionInvoked, onPromptRendering, onPromptRendered, textGenerationService),
            _ => throw new ArgumentException("Invalid filter type")
        };
    }

    private Kernel GetKernelWithSyncFilters(
        Action<FunctionInvokingContext>? onFunctionInvoking = null,
        Action<FunctionInvokedContext>? onFunctionInvoked = null,
        Action<PromptRenderingContext>? onPromptRendering = null,
        Action<PromptRenderedContext>? onPromptRendered = null,
        ITextGenerationService? textGenerationService = null)
    {
        var builder = Kernel.CreateBuilder();
        var functionFilter = new FakeFunctionFilter(onFunctionInvoking, onFunctionInvoked);
        var promptFilter = new FakePromptFilter(onPromptRendering, onPromptRendered);

        // Add function filter before kernel construction
        builder.Services.AddSingleton<IFunctionFilter>(functionFilter);

        if (textGenerationService is not null)
        {
            builder.Services.AddSingleton<ITextGenerationService>(textGenerationService);
        }

        var kernel = builder.Build();

        // Add prompt filter after kernel construction
        kernel.PromptFilters.Add(promptFilter);

        return kernel;
    }

    private Kernel GetKernelWithAsyncFilters(
        Action<FunctionInvokingContext>? onFunctionInvoking = null,
        Action<FunctionInvokedContext>? onFunctionInvoked = null,
        Action<PromptRenderingContext>? onPromptRendering = null,
        Action<PromptRenderedContext>? onPromptRendered = null,
        ITextGenerationService? textGenerationService = null)
    {
        var builder = Kernel.CreateBuilder();
        var functionFilter = new FakeAsyncFunctionFilter(onFunctionInvoking, onFunctionInvoked);
        var promptFilter = new FakeAsyncPromptFilter(onPromptRendering, onPromptRendered);

        // Add function filter before kernel construction
        builder.Services.AddSingleton<IAsyncFunctionFilter>(functionFilter);

        if (textGenerationService is not null)
        {
            builder.Services.AddSingleton<ITextGenerationService>(textGenerationService);
        }

        var kernel = builder.Build();

        // Add prompt filter after kernel construction
        kernel.PromptFilters.Add(promptFilter);

        return kernel;
    }

    private Mock<ITextGenerationService> GetMockTextGeneration()
    {
        var mockTextGeneration = new Mock<ITextGenerationService>();
        mockTextGeneration
            .Setup(m => m.GetTextContentsAsync(It.IsAny<string>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TextContent> { new("result text") });

        mockTextGeneration
            .Setup(s => s.GetStreamingTextContentsAsync(It.IsAny<string>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
            .Returns(new List<StreamingTextContent>() { new("result chunk") }.ToAsyncEnumerable());

        return mockTextGeneration;
    }

    private sealed class FakeAsyncFunctionFilter(
        Action<FunctionInvokingContext>? onFunctionInvoking = null,
        Action<FunctionInvokedContext>? onFunctionInvoked = null) : IAsyncFunctionFilter
    {
        private readonly Action<FunctionInvokingContext>? _onFunctionInvoking = onFunctionInvoking;
        private readonly Action<FunctionInvokedContext>? _onFunctionInvoked = onFunctionInvoked;

        public async Task OnFunctionInvokedAsync(FunctionInvokedContext context) =>
            await Task.Run(() => this._onFunctionInvoked?.Invoke(context));

        public async Task OnFunctionInvokingAsync(FunctionInvokingContext context) =>
            await Task.Run(() => this._onFunctionInvoking?.Invoke(context));
    }

    private sealed class FakeFunctionFilter(
        Action<FunctionInvokingContext>? onFunctionInvoking = null,
        Action<FunctionInvokedContext>? onFunctionInvoked = null) : IFunctionFilter
    {
        private readonly Action<FunctionInvokingContext>? _onFunctionInvoking = onFunctionInvoking;
        private readonly Action<FunctionInvokedContext>? _onFunctionInvoked = onFunctionInvoked;

        public void OnFunctionInvoked(FunctionInvokedContext context) =>
            this._onFunctionInvoked?.Invoke(context);

        public void OnFunctionInvoking(FunctionInvokingContext context) =>
            this._onFunctionInvoking?.Invoke(context);
    }

    private sealed class FakeAsyncPromptFilter(
        Action<PromptRenderingContext>? onPromptRendering = null,
        Action<PromptRenderedContext>? onPromptRendered = null) : IAsyncPromptFilter
    {
        private readonly Action<PromptRenderingContext>? _onPromptRendering = onPromptRendering;
        private readonly Action<PromptRenderedContext>? _onPromptRendered = onPromptRendered;

        public async Task OnPromptRenderedAsync(PromptRenderedContext context) =>
            await Task.Run(() => this._onPromptRendered?.Invoke(context));

        public async Task OnPromptRenderingAsync(PromptRenderingContext context) =>
            await Task.Run(() => this._onPromptRendering?.Invoke(context));
    }

    private sealed class FakePromptFilter(
        Action<PromptRenderingContext>? onPromptRendering = null,
        Action<PromptRenderedContext>? onPromptRendered = null) : IPromptFilter
    {
        private readonly Action<PromptRenderingContext>? _onPromptRendering = onPromptRendering;
        private readonly Action<PromptRenderedContext>? _onPromptRendered = onPromptRendered;

        public void OnPromptRendered(PromptRenderedContext context) =>
            this._onPromptRendered?.Invoke(context);

        public void OnPromptRendering(PromptRenderingContext context) =>
            this._onPromptRendering?.Invoke(context);
    }

    public enum FilterType
    {
        Sync,
        Async
    }
}
