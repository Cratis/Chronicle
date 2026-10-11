// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_clause_generation_is_unregistered : given.a_projection_with_generations
{
    Exception _error;

    void Establish() => _schemas = [_schemas[0]];

    async Task Because() => _error = await Catch.Exception(async () => _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas));

    [Fact] void should_fail_rather_than_substitute_another_schema() => _error.ShouldBeOfExactType<PinnedEventTypeGenerationUnavailable>();
}
