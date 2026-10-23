// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer whose definition is on another event sequence than the one its grain key addresses - the activation
/// a caller causes by addressing a reactor on an inbox with the event log, for instance.
/// </summary>
public class an_observer_under_a_mismatching_key : an_observer
{
    protected static readonly EventSequenceId _definitionEventSequenceId = "inbox-another-store";

    protected TestStorageStats _definitionStorageStats => _silo.StorageManager.GetStorageStats(nameof(ObserverDefinition))!;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            Identifier = _observerId,
            RunningState = ObserverRunningState.Active,
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = 43UL
        };
        _definitionStorage.State = _definitionStorage.State with { EventSequenceId = _definitionEventSequenceId };
        await Crash();
        _definitionStorageStats.ResetCounts();
        _jobsManager.ClearReceivedCalls();
    }
}
