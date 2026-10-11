// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_compatibility_clauses_name_two_generations_of_one_type : given.a_builder_with_generations
{
    Exception _error;

    void Establish()
    {
        _builder.From<First>();
        _builder.Join<Second>(join => join.On(model => model.Name));
    }

    void Because() => _error = Catch.Exception(() => _builder.Build());

    [Fact] void should_keep_compatibility_registration_unchanged() => _error.ShouldBeNull();
}
