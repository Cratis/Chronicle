// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_a_page_event_fails_under_a_shared_composite_key : given.a_page_subscriber
{
    Key _compositeKey;
    ObserverSubscriberResult _result;

    void Establish()
    {
        var value = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)value;
        dictionary["Tenant"] = "tenant-1";
        dictionary["Account"] = "account-1";
        _compositeKey = new Key(value, ArrayIndexers.NoIndexers);

        _events = new[] { "B", "A", "A", "B" }.Select((partition, index) =>
            AppendedEvent.EmptyWithEventSequenceNumber((ulong)index + 1) with
            {
                Context = EventContext.Empty with { SequenceNumber = (ulong)index + 1, EventSourceId = partition }
            }).ToArray();
        _cachedPipeline.Configure().Handle(Arg.Any<AppendedEvent>()).Returns(call =>
        {
            var @event = call.Arg<AppendedEvent>();
            _handled.Add(@event.Context.SequenceNumber);
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.HasChanges.Returns(_hasChanges);
            changeset.HasBeenRemoved().Returns(@event.Context.SequenceNumber == (EventSequenceNumber)3UL);
            var state = new ExpandoObject();
            ((IDictionary<string, object?>)state)["position"] = @event.Context.SequenceNumber.Value;
            changeset.CurrentState.Returns(state);
            return new ProjectionEventContext(_compositeKey, @event, changeset, ProjectionOperationType.From, false)
            {
                IsNewInstance = @event.Context.SequenceNumber == (EventSequenceNumber)1UL
            };
        });
        _cachedPipeline.Configure().Handle(_events[2]).Returns(Task.FromException<ProjectionEventContext>(new InvalidOperationException("boom")));
    }

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_stop_before_the_later_source_run() => _handled.ShouldEqual(new EventSequenceNumber[] { 1, 2 });
    [Fact] void should_return_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_attribute_the_failure_to_its_event_source() => _result.FailedPartition.ShouldEqual((Key)"A");
    [Fact] void should_not_attribute_the_failure_to_the_composite_key() => _result.FailedPartition.ShouldNotEqual(_compositeKey);
    [Fact] void should_name_the_failing_sequence() => _result.FailedAt.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_handled_prefix_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)2UL);
}
