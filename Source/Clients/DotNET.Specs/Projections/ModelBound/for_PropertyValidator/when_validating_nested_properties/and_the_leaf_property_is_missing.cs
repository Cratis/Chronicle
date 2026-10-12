// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_SetBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_nested_properties;

public class and_the_leaf_property_is_missing : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => PropertyValidator.ValidatePropertyExists<NestedReadModel>("Meta.Missing"));

    [Fact] void should_reject_the_path() => _result.ShouldBeOfExactType<InvalidPropertyForType>();
}
