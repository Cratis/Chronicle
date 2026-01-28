// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator;

public class when_validating_property_on_record_type : Specification
{
    Exception? _result;

    void Because() => _result = Catch.Exception(() => PropertyValidator.ValidatePropertyExists<TestRecord>("Count"));

    [Fact] void should_not_throw_exception() => _result.ShouldBeNull();

    record TestRecord(int Count, string Name);
}
