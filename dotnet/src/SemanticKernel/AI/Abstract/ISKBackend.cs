// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.AI.Abstract;

public interface ISKBackend
{
    Task<string> InvokeAsync(string input, IDictionary<string, object> settings, CancellationToken cancellationToken = default);
}
