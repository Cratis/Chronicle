// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch;

public class with_an_invalid_closing_property : given.a_batch_with_a_closing_constraint
{
    ConstraintValidationResult _closing;
    ConstraintValidationResult _later;

    void Establish()
    {
        var definition = new ClosesStreamConstraintDefinition("closing", ["Closed"], _scope.Dimensions, [], "missing");
        _validation = new([new ClosesStreamConstraintValidator(definition, _storage), new ClosedStreamConstraintValidator(_storage, [definition.Dimensions], [definition])]);
    }

    async Task Because()
    {
        _closing = await ContextFor("Closed").Validate();
        _later = await ContextFor("Ordinary").Validate();
    }

    [Fact] void should_refuse_the_invalid_closing_event() => _closing.IsValid.ShouldBeFalse();
    [Fact] void should_not_publish_a_batch_closure_for_an_invalid_event() => _later.IsValid.ShouldBeTrue();
}
