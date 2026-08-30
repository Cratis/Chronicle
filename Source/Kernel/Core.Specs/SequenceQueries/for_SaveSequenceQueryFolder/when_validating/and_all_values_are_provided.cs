// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQueryFolder.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<SaveSequenceQueryFolder> _scenario = ChronicleCommandScenario.For<SaveSequenceQueryFolder>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new SaveSequenceQueryFolder("some-event-store", "some-folder", SequenceQueryScope.User, "some-namespace", "some/path"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
