// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public static class ReducerWatchReadiness
{
    public static async Task WaitFor(IEventStore store, ReducerFrames watch, ReducerFrames reactor, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        var acknowledgements = new List<Task>();
        try
        {
            while (true)
            {
                // Fresh keys retry missed pre-subscription frames without abandoning slow acknowledgements.
                // Scenario assertions select their own keys and ignore these sentinel keys.
                var key = $"ready-{Guid.NewGuid():N}";
                var result = await store.EventLog.Append(key, new Reduction(1)).WaitAsync(timeout.Token);
                result.IsSuccess.ShouldBeTrue();
                acknowledgements.Add(Task.WhenAll(
                    watch.WaitFor(key, ReadModelChangeType.Added, result.SequenceNumber, timeout.Token),
                    reactor.WaitFor(key, ReadModelChangeType.Added, result.SequenceNumber, timeout.Token)));
                if (await WaitForAny(acknowledgements, timeout.Token)) return;
            }
        }
        finally
        {
            await timeout.CancelAsync();
        }
    }

    static async Task<bool> WaitForAny(List<Task> acknowledgements, CancellationToken cancellationToken)
    {
        using var retry = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        retry.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            // This timer permits another sentinel; every acknowledgement keeps the overall deadline.
            var completed = await Task.WhenAny(acknowledgements).WaitAsync(retry.Token);
            await completed;
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
