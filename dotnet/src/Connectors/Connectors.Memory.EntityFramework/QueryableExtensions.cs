// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System;

namespace Microsoft.SemanticKernel.Connectors.EntityFramework;

internal static class QueryableExtensions
{
    internal static IQueryable<TEntity> FilterByIds<TEntity>(this IQueryable<TEntity> source, List<string> ids, string idPropertyName)
    {
        if (ids is not { Count: > 0 })
        {
            return source;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var property = Expression.Property(parameter, idPropertyName);

        var idsExpression = Expression.Constant(ids);
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));

        var containsExpression = Expression.Call(containsMethod, idsExpression, property);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(containsExpression, parameter);

        return source.Where(lambda);
    }
}
