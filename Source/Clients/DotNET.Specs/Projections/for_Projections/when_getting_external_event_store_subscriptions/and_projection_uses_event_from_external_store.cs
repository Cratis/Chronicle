// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions;

public class and_projection_uses_event_from_external_store : given.all_dependencies
{
    const string ExternalStoreName = "external-store";
    const string ExternalEventTypeIdValue = "some-event-type-id";

    [EventType]
    [EventStore(ExternalStoreName)]
    class ExternalEvent;

    record MyModel;

    class ProjectionWithExternalEvent : IProjectionFor<MyModel>
    {
        public void Define(IProjectionBuilderFor<MyModel> builder) => builder.From<ExternalEvent>();
    }

    Projections _projections;
    IEnumerable<(string EventStoreName, IEnumerable<EventTypeId> EventTypeIds)> _result;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(ProjectionWithExternalEvent)]);
        _clientArtifacts.ModelBoundProjections.Returns([]);

        _eventTypes.AllClrTypes.Returns(ImmutableList<Type>.Empty.Add(typeof(ExternalEvent)));
        _eventTypes.GetEventTypeFor(typeof(ExternalEvent))
            .Returns(new EventType(ExternalEventTypeIdValue, EventTypeGeneration.First));

        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<MyModel>>(typeof(ProjectionWithExternalEvent))
            .Returns(new ProjectionWithExternalEvent());

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);
    }

    async Task Because()
    {
        await _projections.Discover();
        _result = _projections.GetExternalEventStoreSubscriptions();
    }

    [Fact] void should_return_one_external_event_store() => _result.Count().ShouldEqual(1);
    [Fact] void should_return_the_correct_event_store_name() => _result.Single().EventStoreName.ShouldEqual(ExternalStoreName);
    [Fact] void should_include_the_event_type_id() => _result.Single().EventTypeIds.ShouldContain(new EventTypeId(ExternalEventTypeIdValue));
}
