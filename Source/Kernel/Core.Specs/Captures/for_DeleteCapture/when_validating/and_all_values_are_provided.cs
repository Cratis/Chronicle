// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Captures.for_DeleteCapture.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<DeleteCapture> _scenario = ChronicleCommandScenario.For<DeleteCapture>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Captures.Has(Arg.Any<CaptureId>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new DeleteCapture("some-event-store", Guid.NewGuid()));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
