// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_the_projection_subscribes_to_all_events : given.a_projection_with_generations
{
    Exception _error;

    void Establish() => _definition = _definition with { SubscribesToAllEvents = true };

    async Task Because() => _error = await Catch.Exception(async () => _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas));

    [Fact] void should_refuse_pinned_wildcard_delivery_until_supported() => _error.ShouldBeOfExactType<EventGenerationDeliveryNotSupported>();
}
