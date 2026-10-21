// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class a_request_persisted_before_reader_scopes : given.a_legacy_observer_job
{
    CatchUpObserverRequest _result;
    readonly EventType _eventType = new("event-type", EventTypeGeneration.First);

    void Because() => _result = RoundTrip(new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [_eventType]));

    [Fact] void should_store_no_reader_scope() => _storedRequest.Contains("readerScope").ShouldBeFalse();
    [Fact] void should_store_no_revision() => _storedRequest.Contains("revision").ShouldBeFalse();
    [Fact] void should_preserve_the_observer_key() => _result.ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_preserve_the_event_types() => _result.EventTypes.ShouldContainOnly([_eventType]);
    [Fact] void should_default_the_reader_scope_to_none() => _result.ReaderScope.ShouldBeNull();
    [Fact] void should_default_the_revision_to_none() => _result.Revision.ShouldBeNull();
}
