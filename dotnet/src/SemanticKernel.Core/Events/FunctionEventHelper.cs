// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Events;

internal static class FunctionEventHelper
{
    public static bool ShouldStopInvocation(FunctionInvokingEventArgs? invokingEvent)
    {
        if (invokingEvent is null)
        {
            return false;
        }

        return invokingEvent.IsSkipRequested || invokingEvent.CancelToken.IsCancellationRequested;
    }

    public static bool ShouldStopInvocation(FunctionInvokedEventArgs? invokedEvent)
    {
        if (invokedEvent is null)
        {
            return false;
        }

        return invokedEvent.CancelToken.IsCancellationRequested;
    }
}
