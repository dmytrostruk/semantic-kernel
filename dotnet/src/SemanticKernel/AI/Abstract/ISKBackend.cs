// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.AI.Abstract;

/// <summary>
/// Interface for AI backend providers.
/// </summary>
public interface ISKBackend
{
    /// <summary>
    /// Performs call to AI backend.
    /// </summary>
    /// <param name="input">Input to process by AI backend.</param>
    /// <param name="settings">Settings to pass for AI backend.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<string> InvokeAsync(string input, IDictionary<string, object> settings, CancellationToken cancellationToken = default);
}
