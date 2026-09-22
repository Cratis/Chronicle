// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventTypes.for_RegisterEventTypes.when_validating;

/// <summary>
/// Verifies a wire-backed repeated field remains available to the handler after command validation.
/// </summary>
public class and_types_can_only_be_enumerated_once : Specification
{
    readonly CommandScenario<RegisterEventTypes> _scenario = ChronicleCommandScenario.For<RegisterEventTypes>();
    readonly RegisterEventTypes _command = new(
        "some-event-store",
        new SingleUseEnumerable([new EventTypeRegistration { Type = new EventType { Id = "SomeEvent" }, Schema = "{}" }]),
        false);
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(_command);

    [Fact] void should_be_valid() => _result.ShouldBeValid();
    [Fact] void should_preserve_the_registration_for_the_handler() => _command.Types.Single().Type.Id.ShouldEqual("SomeEvent");

    sealed class SingleUseEnumerable(IEnumerable<EventTypeRegistration> items) : IEnumerable<EventTypeRegistration>
    {
        bool _enumerated;

        public IEnumerator<EventTypeRegistration> GetEnumerator()
        {
            if (_enumerated)
            {
                throw new InvalidOperationException("This sequence can only be enumerated once.");
            }

            _enumerated = true;
            return items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
