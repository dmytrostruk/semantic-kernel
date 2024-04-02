// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System;

[ExcludeFromCodeCoverage]
internal static class EnumerableExtensions
{
    internal static bool IsNotEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable is not ICollection<T> collection || collection.Count != 0;
    }
}
