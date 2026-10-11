// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Retains the scenario's event-log surface for a named client sequence without changing its routing.
/// </summary>
internal class EventLogForSequence : DispatchProxy
{
    IEventSequence _sequence = null!;

    /// <summary>
    /// Creates an event-log facade for the selected sequence.
    /// </summary>
    /// <param name="sequence">The selected client sequence.</param>
    /// <returns>The event-log facade.</returns>
    internal static IEventLog Create(IEventSequence sequence)
    {
        var proxy = Create<IEventLog, EventLogForSequence>();
        ((EventLogForSequence)proxy)._sequence = sequence;
        return proxy;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        try
        {
            return targetMethod!.Invoke(_sequence, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
