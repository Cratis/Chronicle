// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// Records every call made to a grain of the kernel silo, so a specification can show which grains a wake did and did not call.
/// </summary>
public class GrainCallRecorder : IIncomingGrainCallFilter
{
    readonly ConcurrentQueue<RecordedGrainCall> _calls = new();

    /// <summary>
    /// Gets a snapshot of the calls recorded so far, in the order they were made.
    /// </summary>
    public IReadOnlyList<RecordedGrainCall> Calls => [.. _calls];

    /// <inheritdoc/>
    public Task Invoke(IIncomingGrainCallContext context)
    {
        _calls.Enqueue(new(context.InterfaceName ?? string.Empty, context.MethodName ?? string.Empty, context.TargetId.ToString()));
        return context.Invoke();
    }

    /// <summary>
    /// Counts the calls to a method of a grain interface that were made to grains whose identity contains the given text.
    /// </summary>
    /// <param name="interfaceName">The name of the grain interface, such as IObserverAlerts.</param>
    /// <param name="methodName">The name of the method, or null for every method of the interface.</param>
    /// <param name="targetContains">Text the identity of the called grain contains, ignoring case.</param>
    /// <returns>The number of calls.</returns>
    public int Count(string interfaceName, string? methodName, string targetContains) =>
        Calls.Count(call =>
            call.InterfaceName.EndsWith(interfaceName, StringComparison.Ordinal) &&
            (methodName is null || call.MethodName == methodName) &&
            call.Target.Contains(targetContains, StringComparison.OrdinalIgnoreCase));
}
