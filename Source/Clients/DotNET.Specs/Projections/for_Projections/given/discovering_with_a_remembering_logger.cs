// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_Projections.given;

/// <summary>
/// Discovery of the given artifacts, with a logger that remembers its warnings.
/// </summary>
public class discovering_with_a_remembering_logger : all_dependencies
{
    protected a_logger_that_remembers_warnings _logger;
    protected Projections _projections;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(RootRemoved), typeof(ChildRemoved)]);
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");
        _logger = new();

        _clientArtifacts.Projections.Returns([]);
        _clientArtifacts.ModelBoundProjections.Returns([]);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            _logger);
    }

    [EventType]
    public record RootRemoved(string Id);

    [EventType]
    public record ChildRemoved(string Id);

    public record Item(string Id);
}
