// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class with_pinned_delivery : given.all_dependencies
{
    Projections _projections;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(and_there_are_two_projections.FirstProjection)]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<and_there_are_two_projections.FirstModel>>(typeof(and_there_are_two_projections.FirstProjection)).Returns(new and_there_are_two_projections.FirstProjection());
        _projections = new(_eventStore, _eventTypes, _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance) { GenerationDelivery = EventGenerationDelivery.Pinned };
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_opt_the_definition_into_pinned_delivery() => _projections.Definitions.Single().GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
}
