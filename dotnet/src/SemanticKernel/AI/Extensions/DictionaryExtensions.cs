// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.SemanticKernel.AI.Extensions;

internal static class DictionaryExtensions
{
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

            if (property != null)
            {
                property.SetValue(result, field.Value);
            }
        }

        return result;
    }
}
