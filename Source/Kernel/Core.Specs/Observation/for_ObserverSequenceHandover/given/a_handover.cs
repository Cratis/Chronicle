// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.given;

public class a_handover : Specification
{
    protected static readonly ObserverId _observerId = "an-observer";
    protected static readonly EventSequenceId _from = "from-sequence";
    protected static readonly EventSequenceId _to = "to-sequence";

    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IObserverStateStorage _observers;
    protected IObserverSequenceCheckpointsStorage _checkpoints;
    protected IFailedPartitionsStorage _failedPartitions;
    protected ILogger<Observer> _logger;
    protected ObserverState _current;
    protected FailedPartitions _failures;
    protected SequenceHandoverResult _result;

    void Establish()
    {
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _observers = Substitute.For<IObserverStateStorage>();
        _checkpoints = Substitute.For<IObserverSequenceCheckpointsStorage>();
        _failedPartitions = Substitute.For<IFailedPartitionsStorage>();
        _logger = Substitute.For<ILogger<Observer>>();
        _namespaceStorage.Observers.Returns(_observers);
        _namespaceStorage.ObserverSequenceCheckpoints.Returns(_checkpoints);
        _namespaceStorage.FailedPartitions.Returns(_failedPartitions);

        _current = new()
        {
            Identifier = _observerId,
            Owner = _from,
            NextEventSequenceNumber = 43UL,
            LastHandledEventSequenceNumber = 42UL
        };
        _failures = new();
        _observers.Get(_observerId).Returns(_ => _current);
        _observers.HandOver(_observerId, Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>()).Returns(true);
        _failedPartitions.GetFor(_observerId).Returns(_ => _failures);
    }

    protected Task<SequenceHandoverResult> HandOver() => ObserverSequenceHandover.HandOver(_namespaceStorage, _observerId, _from, _to, _logger);
}
