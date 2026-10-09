// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Results;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.EventSources.for_RegisterEventSourcesValidator.when_registering_definitions;

public class and_event_source_names_are_duplicated : Specification
{
    RegisterEventSourcesValidator _validator;
    RegisterEventSources _command;
    ValidationResult _result;

    void Establish()
    {
        _validator = new();
        _command = new("test-store",
        [
            new Contracts.EventSources.EventSourceDefinition { Name = "ShoppingCart", Description = "First definition" },
            new Contracts.EventSources.EventSourceDefinition { Name = "ShoppingCart", Description = "Second definition" }
        ]);
    }

    void Because() => _result = _validator.Validate(_command);

#pragma warning disable xUnit1004 // Chronicle#4658 requires a skipped spec for a reproduced conformance gap.
    [Fact(Skip = "Chronicle#4658: Registration validation accepts duplicate event source names in the same request.")] void should_refuse_the_duplicate_event_source_name() => _result.IsValid.ShouldBeFalse();
#pragma warning restore xUnit1004
}
