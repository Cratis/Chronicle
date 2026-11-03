// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_redacting;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_another_redaction_won_after_reading(ReplicaSetMongoDBFixture fixture) : given.a_second_writer_parked_before_its_update(fixture)
{
    CorrelationId _firstCorrelation;
    CorrelationId _lateCorrelation;
    DateTimeOffset _firstOccurred;
    DateTimeOffset _lateOccurred;
    AppendedEvent _late;
    Event _stored;

    async Task Establish()
    {
        (await _storage.AppendMany([EventAt(0)])).IsSuccess.ShouldBeTrue();
        _firstCorrelation = CorrelationId.New();
        _lateCorrelation = CorrelationId.New();
        _firstOccurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _lateOccurred = _firstOccurred.AddHours(1);
    }

    async Task Because()
    {
        Interlocked.Exchange(ref _armed, 1);
        var late = Task.Run(() => _secondWriter.Redact(0, "late", _lateCorrelation, [], [], _lateOccurred));
        try
        {
            await _parked.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await _storage.Redact(0, "first", _firstCorrelation, [], [], _firstOccurred);
        }
        finally
        {
            _release.Set();
            _late = await late;
        }

        _stored = await _collection.Find(@event => @event.SequenceNumber == 0).SingleAsync();
    }

    [Fact] void should_have_parked_the_second_writer() => (_parked.Task.IsCompleted && !_gateTimedOut).ShouldBeTrue();
    [Fact] void should_keep_the_first_redaction_correlation() => _stored.CorrelationId.ShouldEqual(_firstCorrelation);
    [Fact] void should_signal_already_redacted_to_the_late_writer() => _late.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
}
