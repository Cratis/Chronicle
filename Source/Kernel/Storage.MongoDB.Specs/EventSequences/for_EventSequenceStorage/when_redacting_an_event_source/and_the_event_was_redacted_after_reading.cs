// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_redacting_an_event_source;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_event_was_redacted_after_reading(ReplicaSetMongoDBFixture fixture, ITestOutputHelper output) : given.a_second_writer_parked_before_its_update(fixture)
{
    CorrelationId _pointCorrelation;
    CorrelationId _bulkCorrelation;
    DateTimeOffset _pointOccurred;
    DateTimeOffset _bulkOccurred;
    IEnumerable<EventType> _affected;
    Event _stored;

    async Task Establish()
    {
        (await _storage.AppendMany([EventAt(0)])).IsSuccess.ShouldBeTrue();
        _pointCorrelation = CorrelationId.New();
        _bulkCorrelation = CorrelationId.New();
        _pointOccurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _bulkOccurred = _pointOccurred.AddHours(1);
    }

    async Task Because()
    {
        Interlocked.Exchange(ref _armed, 1);
        var bulk = Task.Run(() => _secondWriter.Redact((EventSourceId)"some-source", "bulk-reason", null, _bulkCorrelation, [], [], _bulkOccurred));
        try
        {
            await _parked.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await _storage.Redact(0, "point-reason", _pointCorrelation, [], [], _pointOccurred);
        }
        finally
        {
            _release.Set();
            _affected = await bulk;
        }

        _stored = await _collection.Find(@event => @event.SequenceNumber == 0).SingleAsync();
        output.WriteLine($"Point correlation: {_pointCorrelation}; bulk correlation: {_bulkCorrelation}; stored correlation: {_stored.CorrelationId}");
        output.WriteLine($"Point occurred: {_pointOccurred:O}; bulk occurred: {_bulkOccurred:O}; stored occurred: {_stored.Occurred:O}");
        output.WriteLine($"Affected types: {string.Join(", ", _affected.Select(type => type.Id))}");
    }

    [Fact] void should_have_parked_the_second_writer() => (_parked.Task.IsCompleted && !_gateTimedOut).ShouldBeTrue();
    [Fact] void should_keep_the_first_redaction_correlation() => _stored.CorrelationId.ShouldEqual(_pointCorrelation);
    [Fact] void should_keep_the_first_redaction_time() => _stored.Occurred.ShouldEqual(_pointOccurred);
}
