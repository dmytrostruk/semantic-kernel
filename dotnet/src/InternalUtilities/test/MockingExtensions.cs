// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Reflection;

internal static class MockingExtensions
{
    public static object Protected(this object target, string methodName, params object[] args)
    {
        var type = target.GetType();

        var method = type
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => x.Name == methodName).Single();

        return method.Invoke(target, args) ?? throw new InvalidOperationException($"'{methodName}' cannot be mocked");
    }
}
