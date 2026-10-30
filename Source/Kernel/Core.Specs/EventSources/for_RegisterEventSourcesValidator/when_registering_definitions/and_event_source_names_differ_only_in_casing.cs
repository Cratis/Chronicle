// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Results;

namespace Cratis.Chronicle.EventSources.for_RegisterEventSourcesValidator.when_registering_definitions;

public class and_event_source_names_differ_only_in_casing : Specification
{
    RegisterEventSourcesValidator _validator;
    RegisterEventSources _command;
    ValidationResult _result;

    void Establish()
    {
        _validator = new();
        _command = new("test-store",
        [
            new Contracts.EventSources.EventSourceDefinition { Name = "ShoppingCart" },
            new Contracts.EventSources.EventSourceDefinition { Name = "shoppingcart" }
        ]);
    }

    void Because() => _result = _validator.Validate(_command);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
