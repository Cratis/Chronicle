// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

/// <summary>
/// Gives these fault-injection specs their own silo and service registrations.
/// </summary>
public class PatternCaptureFixture : ChronicleInProcessFixture
{
    /// <summary>
    /// Signals persisted observer progress without polling or depending on Arc's ambient service provider.
    /// </summary>
    /// <param name="storage">The real storage used by the observer grains.</param>
    /// <param name="logger">The logger.</param>
    public sealed class SignalingObserverStorage(IStorage storage, ILogger<ObserverStateGrainStorageProvider> logger) : ObserverStateGrainStorageProvider(storage, logger), IDisposable
    {
        readonly ConcurrentDictionary<ObserverKey, ReplaySubject<EventSequenceNumber>> _positions = new();

        /// <inheritdoc/>
        public override async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            var key = ObserverKey.Parse(grainId.Key.ToString()!);
            var position = ((ObserverState)(object)grainState.State!).LastHandledEventSequenceNumber;
            await base.WriteStateAsync(stateName, grainId, grainState);
            _positions.GetOrAdd(key, _ => new(1)).OnNext(position);
        }

        /// <summary>
        /// Waits for a specific observer to persist that it handled the appended event.
        /// </summary>
        /// <param name="key">The observer in the target namespace and sequence.</param>
        /// <param name="sequenceNumber">The appended event's sequence number.</param>
        /// <returns>A task completed only by the observer's persisted progress.</returns>
        public async Task<EventSequenceNumber> WaitForHandled(ObserverKey key, EventSequenceNumber sequenceNumber)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await _positions.GetOrAdd(key, _ => new(1))
                .FirstAsync(position => position.IsActualValue && position >= sequenceNumber)
                .ToTask(timeout.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var position in _positions.Values) position.Dispose();
        }
    }
}
