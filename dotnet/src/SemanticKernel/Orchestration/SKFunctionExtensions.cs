// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.SemanticKernel.Orchestration;

/// <summary>
/// Class that holds extension methods for objects implementing ISKFunction.
/// </summary>
public static class SKFunctionExtensions
{
    /// <summary>
    /// Execute a function with a custom set of context variables.
    /// Use case: template engine: semantic function with custom input variable.
    /// </summary>
    /// <param name="function">Function to execute</param>
    /// <param name="input">Custom function input</param>
    /// <param name="memory">Semantic memory</param>
    /// <param name="skills">Available skills</param>
    /// <param name="log">App logger</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The temporary context</returns>
    public static async Task<SKContext> InvokeWithCustomInputAsync(this ISKFunction function,
        ContextVariables input,
        ISemanticTextMemory memory,
        IReadOnlySkillCollection? skills,
        ILogger log,
        CancellationToken cancellationToken)
    {
        var tmpContext = new SKContext(input, memory, skills, log, cancellationToken);
        try
        {
            await function.InvokeAsync(tmpContext);
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            log.LogError(ex, "Something went wrong when invoking function with custom input: {0}.{1}. Error: {2}", function.SkillName,
                function.Name, ex.Message);
            tmpContext.Fail(ex.Message, ex);
        }

        return tmpContext;
    }
}
