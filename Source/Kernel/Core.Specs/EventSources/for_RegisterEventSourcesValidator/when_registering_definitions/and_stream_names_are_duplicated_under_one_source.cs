// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Results;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.EventSources.for_RegisterEventSourcesValidator.when_registering_definitions;

public class and_stream_names_are_duplicated_under_one_source : Specification
{
    RegisterEventSourcesValidator _validator;
    RegisterEventSources _command;
    ValidationResult _result;

    void Establish()
    {
        _validator = new();
        _command = new("test-store",
        [
            new Contracts.EventSources.EventSourceDefinition
            {
                Name = "ShoppingCart",
                Streams =
                [
                    new Contracts.EventSources.EventStreamDefinition { Name = "Items", Description = "First stream" },
                    new Contracts.EventSources.EventStreamDefinition { Name = "Items", Description = "Second stream" }
                ]
            }
        ]);
    }

    void Because() => _result = _validator.Validate(_command);

#pragma warning disable xUnit1004 // Chronicle#4658 requires a skipped spec for a reproduced conformance gap.
    [Fact(Skip = "Chronicle#4658: Registration validation accepts duplicate stream names under one event source.")] void should_refuse_the_duplicate_stream_name() => _result.IsValid.ShouldBeFalse();
#pragma warning restore xUnit1004
}
