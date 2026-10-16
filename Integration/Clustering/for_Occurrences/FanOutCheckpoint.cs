// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

/// <summary>
/// Pauses only after a real prefix commit, so a silo can die with unfinished durable fan-out.
/// </summary>
public sealed class FanOutCheckpoint
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _armed;
    public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Arm() => Interlocked.Exchange(ref _armed, 1);
    public void Release() => _release.TrySetResult();
    public ILifecycleFanOutStorage Decorate(ILifecycleFanOutStorage inner) => new PausedPrefix(inner, this);

    sealed class PausedPrefix(ILifecycleFanOutStorage inner, FanOutCheckpoint checkpoint) : ILifecycleFanOutStorage
    {
        public Task<LifecycleFanOutIntent?> Get(OccurrenceId id) => inner.Get(id);
        public Task<bool> TryInsert(LifecycleFanOutIntent intent) => inner.TryInsert(intent);
        public Task<IEnumerable<LifecycleFanOutIntent>> GetPending(int max) => inner.GetPending(max);
        public async Task Advance(OccurrenceId id, int progress)
        {
            await inner.Advance(id, progress);
            if (progress == 100 && Interlocked.CompareExchange(ref checkpoint._armed, 0, 1) == 1)
            {
                checkpoint.Reached.TrySetResult();
                await checkpoint._release.Task.WaitAsync(TimeSpan.FromSeconds(120));
            }
        }
    }
}
