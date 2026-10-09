// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Targets.for_target_schema.when_resolving;

public class and_the_target_is_a_read_model : given.a_read_model_target
{
    void Because() => _result = _target.GetTargetSchema();

    [Fact] void should_resolve_the_latest_registered_schema() => _result.ShouldEqual(_latestSchema);
    [Fact] void should_preserve_the_existing_read_model_schema_resolution() => _result.ShouldEqual(_readModel.GetSchemaForLatestGeneration());
    [Fact] void should_not_add_a_generation() => _readModel.Schemas.Count.ShouldEqual(2);
}
