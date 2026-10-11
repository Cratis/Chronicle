// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_SetBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_nested_properties;

public class and_the_casing_differs : Specification
{
    string _result;

    void Because() => _result = PropertyValidator.ValidatePropertyExists<NestedReadModel>("meta.topic");

    [Fact] void should_match_each_segment_case_insensitively() => _result.ShouldEqual("meta.topic");
}
