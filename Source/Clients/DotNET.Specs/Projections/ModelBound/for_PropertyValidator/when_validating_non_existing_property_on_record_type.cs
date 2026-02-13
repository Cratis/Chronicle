// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator;

public class when_validating_non_existing_property_on_record_type : Specification
{
    Exception? _result;

    void Because() => _result = Catch.Exception(() => PropertyValidator.ValidatePropertyExists<TestRecord>("NonExistingProperty"));

    [Fact] void should_throw_invalid_property_for_type() => _result.ShouldBeOfExactType<InvalidPropertyForType>();
    [Fact] void should_include_type_in_message() => _result?.Message.ShouldContain(typeof(TestRecord).FullName);
    [Fact] void should_include_property_name_in_message() => _result?.Message.ShouldContain("NonExistingProperty");

    record TestRecord(int Count, string Name);
}
