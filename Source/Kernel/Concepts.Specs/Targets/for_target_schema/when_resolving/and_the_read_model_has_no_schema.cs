// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Targets.for_target_schema.when_resolving;

public class and_the_read_model_has_no_schema : given.a_read_model_target
{
    Exception _error;

    void Establish() => _readModel.Schemas.Clear();

    void Because() => _error = Catch.Exception(() => _target.GetTargetSchema());

    [Fact] void should_fail_instead_of_inventing_an_empty_schema() => _error.ShouldBeOfExactType<MissingSchemaForReadModel>();
    [Fact] void should_not_register_a_generation() => _readModel.Schemas.ShouldBeEmpty();
}
