// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_from_clauses_name_different_generations : given.a_projection_with_generations
{
    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        Project(_projection, Event(_first, "original"));
        Project(_projection, Event(_second, "migrated"));
    }

    [Fact] void should_project_the_first_clause_only_from_its_generation() => ((IDictionary<string, object?>)_state)["oldName"].ShouldEqual("original");
    [Fact] void should_project_the_second_clause_only_from_its_generation() => ((IDictionary<string, object?>)_state)["newName"].ShouldEqual("migrated");
}
