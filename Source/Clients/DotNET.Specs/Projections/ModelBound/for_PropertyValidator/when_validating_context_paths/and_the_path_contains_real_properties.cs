// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_context_paths;

public class and_the_path_contains_real_properties : Specification
{
    string _result;

    void Because() => _result = PropertyValidator.ValidateContextPropertyPath<Context>("value.week.length");

    [Fact] void should_prefer_real_members_and_canonicalize_their_names() => _result.ShouldEqual("Value.Week.Length");

    record Context(Receiver Value);
    record Receiver(string Week);
}
