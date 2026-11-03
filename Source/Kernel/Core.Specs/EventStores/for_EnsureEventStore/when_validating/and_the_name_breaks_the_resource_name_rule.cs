// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_validating;

public class and_the_name_breaks_the_resource_name_rule : Specification
{
    static readonly string[] _names = ["a+es+b", "a#b", "a~0123456789abcdef", "a$b", "a\tb", "es", "ES", "admin", "local", "config"];

    readonly CommandScenario<EnsureEventStore> _scenario = ChronicleCommandScenario.For<EnsureEventStore>();
    List<CommandResult> _results;

    async Task Because()
    {
        _results = [];
        foreach (var name in _names)
        {
            _results.Add(await _scenario.Validate(new EnsureEventStore(name)));
        }
    }

    [Fact] void should_refuse_every_name() => _results.ShouldEachConformTo(_ => !_.IsSuccess);
    [Fact] void should_use_the_rule_message() => _results[0].ValidationResults.Select(_ => _.Message).ShouldContain(ResourceNames.EventStoreRuleMessage);
    [Fact] void should_not_echo_the_name() => _results[0].ValidationResults.Select(_ => _.Message).ShouldEachConformTo(_ => !_.Contains("a+es+b"));
}
