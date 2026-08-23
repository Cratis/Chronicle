// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class and_an_updated_definition_is_rejected : given.a_projections_manager
{
    IProjection _projectionBeforeUpdate;
    IProjection _projectionAfterUpdate;
    Exception _exception;

    async Task Because()
    {
        await _manager.Register(
            _eventStore,
            [_firstDefinition],
            [_firstReadModelDefinition],
            [_namespace]);
        _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _projectionBeforeUpdate);

        _projectionFactory
            .Create(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Is(_firstDefinition),
                Arg.Any<ReadModelDefinition>(),
                Arg.Any<IEnumerable<EventTypeSchema>>())
            .ThrowsAsync(new InvalidOperationException("Failed to create the updated projection"));

        _exception = await Catch.Exception(() => _manager.Register(
            _eventStore,
            [_firstDefinition],
            [_firstReadModelDefinition],
            [_namespace]));
        _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _projectionAfterUpdate);
    }

    [Fact] void should_fail_the_update() => _exception.ShouldBeOfExactType<ProjectionDefinitionsRegistrationFailed>();
    [Fact] void should_preserve_the_registered_projection() => _projectionAfterUpdate.ShouldEqual(_projectionBeforeUpdate);
}
