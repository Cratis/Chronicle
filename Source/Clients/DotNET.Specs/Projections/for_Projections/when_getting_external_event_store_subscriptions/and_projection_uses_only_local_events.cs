// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions;

public class and_projection_uses_only_local_events : given.all_dependencies
{
    record MyModel;

    class ProjectionWithLocalEvent : IProjectionFor<MyModel>
    {
        public void Define(IProjectionBuilderFor<MyModel> builder) { }
    }

    Projections _projections;
    IEnumerable<(string EventStoreName, IEnumerable<EventTypeId> EventTypeIds)> _result;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(ProjectionWithLocalEvent)]);
        _clientArtifacts.ModelBoundProjections.Returns([]);

        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<MyModel>>(typeof(ProjectionWithLocalEvent))
            .Returns(new ProjectionWithLocalEvent());

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

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
