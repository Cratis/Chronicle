// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Orleans.TestKit;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReplayManager.given;

public class a_read_model_replay_manager : Specification
{
    protected static readonly DateTimeOffset _firstStarted = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    protected TestKitSilo _silo = new();
    protected TestStorageStats _storageStats;
    protected ReadModelReplayManager _manager;
    protected global::Orleans.Core.IStorage<ReadModelReplayManagerState> _stateStorage;
    protected ReadModelOccurrence[] _seeded = [];

    protected virtual IEnumerable<ReadModelOccurrence> OccurrencesToSeed => [];

    protected static ReadModelOccurrence OccurrenceStartedAt(int daysAfterFirst) => new(
        "some-observer",
        _firstStarted.AddDays(daysAfterFirst),
        new ReadModelType("totals", ReadModelGeneration.First),
        "totals",
        $"totals-revert-{daysAfterFirst}");

    protected static ReplayContext ReplayStartedAt(int daysAfterFirst) => new(
        new ReadModelType("totals", ReadModelGeneration.First),
        "totals",
        $"totals-revert-{daysAfterFirst}",
        _firstStarted.AddDays(daysAfterFirst));

    async Task Establish()
    {
        _seeded = OccurrencesToSeed.ToArray();
        _stateStorage = _silo.StorageManager.GetStorage<ReadModelReplayManagerState>(typeof(ReadModelReplayManager).FullName!);
        _stateStorage.State = new ReadModelReplayManagerState { Occurrences = _seeded.ToList() };
        _manager = await _silo.CreateGrainAsync<ReadModelReplayManager>("some-event-store+default+totals");
        _storageStats = _silo.StorageStats<ReadModelReplayManager, ReadModelReplayManagerState>();
    }
}
