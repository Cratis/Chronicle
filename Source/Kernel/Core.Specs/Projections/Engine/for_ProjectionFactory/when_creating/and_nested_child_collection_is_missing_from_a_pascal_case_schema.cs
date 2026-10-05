// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_nested_child_collection_is_missing_from_a_pascal_case_schema : given.a_child_collection_projection
{
    Exception _exception;

    void Establish() => _definition.Children["notes"].Children["reactions"] = Child();

    async Task Because() => _exception = await Catch.Exception(() => _factory.Create(_eventStore, _namespace, _definition, _readModel, []));

    [Fact] void should_fail_loudly() => _exception.ShouldBeOfExactType<MissingChildCollectionInReadModelSchema>();
}
