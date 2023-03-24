// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.AI.Abstract;

public interface ISKBackend
{
    Task<string> InvokeAsync(string input, ISKBackendSettings settings, CancellationToken cancellationToken = default);
}
