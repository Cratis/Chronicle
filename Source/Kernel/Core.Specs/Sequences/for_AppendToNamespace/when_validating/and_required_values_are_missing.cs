// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace.when_validating;

public class and_required_values_are_missing : Specification
{
    readonly CommandScenario<AppendToNamespace> _scenario = ChronicleCommandScenario.For<AppendToNamespace>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new AppendToNamespace(
        "", "", NamespaceIncarnationId.NotSet, "", "", "", "", "", new EventType("", 1, false), ""));

    [Fact] void should_be_invalid() => _result.ShouldHaveValidationErrors();
    [Fact] void should_reject_the_missing_identity() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("expectedIncarnation");
}
