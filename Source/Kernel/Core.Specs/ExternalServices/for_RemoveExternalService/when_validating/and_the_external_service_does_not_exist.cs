// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.ExternalServices.for_RemoveExternalService.when_validating;

/// <summary>
/// An external service's existence is scoped to the event store it belongs to, so the check is a command-level
/// RuleFor(...).MustAsync(...) rather than a cross-cutting ConceptValidator&lt;ExternalServiceId&gt; - a standalone
/// concept validator would have no visibility into which event store the id needs to exist in.
/// </summary>
public class and_the_external_service_does_not_exist : Specification
{
    readonly CommandScenario<RemoveExternalService> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        storage.GetEventStore(Arg.Any<EventStoreName>()).ExternalServices.Has(Arg.Any<ExternalServiceId>()).Returns(false);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new RemoveExternalService("some-event-store", "some-service"));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
