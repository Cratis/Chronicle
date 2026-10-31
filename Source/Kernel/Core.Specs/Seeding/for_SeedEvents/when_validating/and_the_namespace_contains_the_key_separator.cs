// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Seeding.for_SeedEvents.when_validating;

public class and_the_namespace_contains_the_key_separator : Specification
{
    readonly CommandScenario<SeedEvents> _scenario = ChronicleCommandScenario.For<SeedEvents>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new SeedEvents("some-event-store", [], [], [new Cratis.Chronicle.Contracts.Seeding.NamespacedSeedEntries { Namespace = "Default#x" }]));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_namespace_member() => _result.ValidationResults.SelectMany(result => result.Members).Any(member => member.Contains("amespace", StringComparison.Ordinal)).ShouldBeTrue();
}
