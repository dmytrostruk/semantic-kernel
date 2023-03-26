// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.SemanticKernel.AI.Extensions;

/// <summary>
/// Extensions for <see cref="IDictionary{TKey, TValue}"/> data structure.
/// </summary>
internal static class DictionaryExtensions
{
    /// <summary>
    /// Creates instance of <typeparamref name="T"/> and populates all matching fields.
    /// </summary>
    /// <typeparam name="T">Type of instance to create.</typeparam>
    /// <param name="source">Source <see cref="IDictionary{TKey, TValue}"/> object.</param>
    /// <returns>Created instance of <typeparamref name="T"/> with populated fields.</returns>
    internal static T? ToObject<T>(this IDictionary<string, object> source) where T : class, new()
    {
        if (source == null)
        {
            return null;
        }

        var result = new T();
        var resultType = typeof(T);

        foreach (var field in source)
        {
            var property = resultType.GetProperty(field.Key);

            property?.SetValue(result, field.Value);
        }

        return result;
    }
}
