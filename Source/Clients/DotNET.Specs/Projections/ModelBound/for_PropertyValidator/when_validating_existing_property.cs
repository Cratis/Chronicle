// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator;

public class when_validating_existing_property : Specification
{
    Exception? _result;

    void Because() => _result = Catch.Exception(() => PropertyValidator.ValidatePropertyExists<TestClass>("ExistingProperty"));

    [Fact] void should_not_throw_exception() => _result.ShouldBeNull();

    class TestClass
    {
        public string ExistingProperty { get; set; } = string.Empty;
    }
}
