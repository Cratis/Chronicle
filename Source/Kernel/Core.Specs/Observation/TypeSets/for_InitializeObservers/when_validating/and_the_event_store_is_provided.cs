// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_InitializeObservers.when_validating;

public class and_the_event_store_is_provided : Specification
{
    FluentValidation.Results.ValidationResult _result;

    void Because() => _result = new InitializeObserversValidator().Validate(new InitializeObservers("store", false, false));

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
