// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_context_paths;

public class and_the_receiver_is_a_nullable_date : Specification
{
    string _result;

    void Because() => _result = PropertyValidator.ValidateContextPropertyPath<Context>("Occurred.Week");

    [Fact] void should_accept_the_underlying_date_type() => _result.ShouldEqual("Occurred.Week()");

    record Context(DateOnly? Occurred);
}
