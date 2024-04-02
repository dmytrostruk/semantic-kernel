// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Interface for asynchronous filtering actions during prompt rendering.
/// </summary>
[Experimental("SKEXP0001")]
public interface IAsyncPromptFilter : IPromptFilterBase
{
    /// <summary>
    /// Method which is executed before prompt rendering.
    /// </summary>
    /// <param name="context">Data related to prompt before rendering.</param>
    Task OnPromptRenderingAsync(PromptRenderingContext context);

    /// <summary>
    /// Method which is executed after prompt rendering.
    /// </summary>
    /// <param name="context">Data related to prompt after rendering.</param>
    Task OnPromptRenderedAsync(PromptRenderedContext context);
}
