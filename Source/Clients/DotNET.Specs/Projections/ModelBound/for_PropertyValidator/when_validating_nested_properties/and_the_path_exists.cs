// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_SetBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_nested_properties;

public class and_the_path_exists : Specification
{
    string _result;

    void Because() => _result = PropertyValidator.ValidatePropertyExists<NestedReadModel>("Meta.Topic");

    [Fact] void should_validate_every_segment() => _result.ShouldEqual("Meta.Topic");
}
