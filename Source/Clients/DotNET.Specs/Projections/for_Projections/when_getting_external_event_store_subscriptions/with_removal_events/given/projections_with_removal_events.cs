// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions.with_removal_events.given;

public class projections_with_removal_events : for_Projections.given.all_dependencies
{
    protected const string ExternalStoreName = "external-store";
    protected Projections _projections;
    protected IEnumerable<(string EventStoreName, IEnumerable<EventTypeId> EventTypeIds)> _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([
            typeof(ExternalCreated),
            typeof(ExternalJoined),
            typeof(ExternalRemoved),
            typeof(ExternalRemovedViaJoin),
            typeof(ExternalNestedRemoved),
            typeof(LocalCreated),
            typeof(LocalRemoved)]);
        _eventStore.Name.Returns((EventStoreName)"local-store");
        _clientArtifacts.Projections.Returns([]);
        _clientArtifacts.ModelBoundProjections.Returns([]);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);
    }

    [EventType(nameof(ExternalCreated))]
    [EventStore(ExternalStoreName)]
    public record ExternalCreated(string Id);

    [EventType(nameof(ExternalJoined))]
    [EventStore(ExternalStoreName)]
    public record ExternalJoined(string Id);

    [EventType(nameof(ExternalRemoved))]
    [EventStore(ExternalStoreName)]
    public record ExternalRemoved(string Id);

    [EventType(nameof(ExternalRemovedViaJoin))]
    [EventStore(ExternalStoreName)]
    public record ExternalRemovedViaJoin(string Id);

    [EventType(nameof(ExternalNestedRemoved))]
    [EventStore(ExternalStoreName)]
    public record ExternalNestedRemoved(string Id);

    [EventType(nameof(LocalCreated))]
    public record LocalCreated(string Id);

    [EventType(nameof(LocalRemoved))]
    public record LocalRemoved(string Id);
}
