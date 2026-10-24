// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_a_wildcard_projection_uses_compatibility_release : given.a_projection_with_a_history
{
    readonly List<AppendedEvent> _projected = [];

    async Task Establish()
    {
        var definition = await _projection.GetDefinition();
        _projection.GetDefinition().Returns(definition with
        {
            SubscribesToAllEvents = true,
            GenerationDelivery = EventGenerationDelivery.Pinned
        });
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call =>
            {
                dynamic released = new ExpandoObject();
                released.name = "released-for-compatibility";
                return Task.FromResult(call.ArgAt<IEnumerable<AppendedEvent>>(0)
                    .Select(@event => @event with { Content = (ExpandoObject)released }).ToArray());
            });
        _projection.ProcessForSingleReadModel(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _projected.AddRange(call.ArgAt<IEnumerable<AppendedEvent>>(2));
                return Task.FromResult(call.ArgAt<ExpandoObject>(1));
            });
    }

    async Task Because() => await AllSnapshots();

    [Fact] void should_decrypt_before_the_compatibility_projection() =>
        _projected.TrueForAll(@event => Equals(((IDictionary<string, object?>)@event.Content)["name"], "released-for-compatibility")).ShouldBeTrue();
    [Fact] void should_project_all_events() => _projected.Count.ShouldEqual(4);
    [Fact] void should_release_compliance_once_for_the_history() => _eventCompliance.Received(1)
        .Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>());
}
