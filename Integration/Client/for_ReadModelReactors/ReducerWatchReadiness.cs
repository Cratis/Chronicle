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
        while (true)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            attempt.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                // A missed pre-subscription frame is retried with a fresh generation, never an old acknowledgement.
                // Scenario assertions select their own keys and ignore these sentinel keys.
                var key = $"ready-{Guid.NewGuid():N}";
                var result = await store.EventLog.Append(key, new Reduction(1)).WaitAsync(timeout.Token);
                result.IsSuccess.ShouldBeTrue();
                await Task.WhenAll(
                    watch.WaitFor(key, ReadModelChangeType.Added, result.SequenceNumber, attempt.Token),
                    reactor.WaitFor(key, ReadModelChangeType.Added, result.SequenceNumber, attempt.Token));
                return;
            }
            catch (OperationCanceledException) when (!timeout.IsCancellationRequested)
            {
            }
        }
    }
}
