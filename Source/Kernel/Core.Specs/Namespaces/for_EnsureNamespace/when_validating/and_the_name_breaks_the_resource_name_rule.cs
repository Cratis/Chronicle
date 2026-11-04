// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_validating;

public class and_the_name_breaks_the_resource_name_rule : Specification
{
    static readonly string[] _names = ["a+es+b", "a#b", "a~0123456789abcdef", "a$b", "a\tb", "es", "ES"];

    readonly CommandScenario<EnsureNamespace> _scenario = ChronicleCommandScenario.For<EnsureNamespace>();
    List<CommandResult> _results;
    CommandResult _combination;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because()
    {
        _results = [];
        foreach (var name in _names)
        {
            _results.Add(await _scenario.Validate(new EnsureNamespace("some-event-store", name)));
        }

        _combination = await _scenario.Validate(new EnsureNamespace("Chronicle", "MAIN"));
    }

    [Fact] void should_refuse_every_name() => _results.ShouldEachConformTo(_ => !_.IsSuccess);
    [Fact] void should_refuse_the_cluster_combination() => _combination.IsSuccess.ShouldBeFalse();
    [Fact] void should_use_the_rule_message() => _results[0].ValidationResults.Select(_ => _.Message).ShouldContain(ResourceNames.NamespaceRuleMessage);
}
