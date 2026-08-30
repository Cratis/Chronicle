// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_validating;

/// <summary>
/// EnsureNamespace creates the namespace but never the event store it belongs to, so its EventStore property
/// gets the cross-cutting EventStoreNameValidator existence check - unlike EnsureEventStore's own Name, which
/// opts out because that command's whole purpose is to create the event store.
/// </summary>
public class and_the_event_store_does_not_exist : Specification
{
    readonly CommandScenario<EnsureNamespace> _scenario = ChronicleCommandScenario.For<EnsureNamespace>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(false);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new EnsureNamespace("some-event-store", "some-namespace"));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
