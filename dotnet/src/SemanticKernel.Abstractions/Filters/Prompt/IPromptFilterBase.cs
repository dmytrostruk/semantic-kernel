// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Base interface for filtering actions during prompt rendering.
/// </summary>
[Experimental("SKEXP0001")]
#pragma warning disable CA1040
public interface IPromptFilterBase
#pragma warning restore CA1040
{
}
