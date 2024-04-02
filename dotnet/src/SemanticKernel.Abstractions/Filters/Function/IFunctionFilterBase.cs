// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Base interface for filtering actions during function invocation.
/// </summary>
[Experimental("SKEXP0001")]
#pragma warning disable CA1040
public interface IFunctionFilterBase
#pragma warning restore CA1040
{
}
