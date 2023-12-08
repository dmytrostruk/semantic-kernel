﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

public static class Example74_Filters
{
    public class FirstFunctionFilter : IFunctionFilter
    {
        public void OnFunctionInvoking(FunctionInvokingContext context) =>
            Console.WriteLine($"{nameof(FirstFunctionFilter)}.{nameof(OnFunctionInvoking)} - {context.Function.Name}");

        public void OnFunctionInvoked(FunctionInvokedContext context) =>
            Console.WriteLine($"{nameof(FirstFunctionFilter)}.{nameof(OnFunctionInvoked)} - {context.Function.Name}");
    }

    public class SecondFunctionFilter : IFunctionFilter
    {
        public void OnFunctionInvoking(FunctionInvokingContext context) =>
            Console.WriteLine($"{nameof(SecondFunctionFilter)}.{nameof(OnFunctionInvoking)} - {context.Function.Name}");

        public void OnFunctionInvoked(FunctionInvokedContext context) =>
            Console.WriteLine($"{nameof(SecondFunctionFilter)}.{nameof(OnFunctionInvoked)} - {context.Function.Name}");
    }

    public class FirstPromptFilter : IPromptFilter
    {
        public void OnPromptRendering(PromptRenderingContext context) =>
            Console.WriteLine($"{nameof(FirstPromptFilter)}.{nameof(OnPromptRendering)} - {context.Function.Name}");

        public void OnPromptRendered(PromptRenderedContext context) =>
            Console.WriteLine($"{nameof(FirstPromptFilter)}.{nameof(OnPromptRendered)} - {context.Function.Name}");
    }

    public static async Task RunAsync()
    {
        var serviceCollection = new ServiceCollection();

        var services = new ServiceCollection();

        services.AddOpenAIChatCompletion(TestConfiguration.OpenAI.ChatModelId, TestConfiguration.OpenAI.ApiKey);

        services.AddSingleton<IFunctionFilter, FirstFunctionFilter>();
        services.AddSingleton<IFunctionFilter, SecondFunctionFilter>();
        services.AddSingleton<IPromptFilter, FirstPromptFilter>();

        Kernel kernel = new(services.BuildServiceProvider());

        var function = kernel.CreateFunctionFromPrompt("What is Seattle", functionName: "MyFunction");
        var result = await kernel.InvokeAsync(function);

        Console.WriteLine();
        Console.WriteLine(result);

        Console.ReadKey();
    }
}
