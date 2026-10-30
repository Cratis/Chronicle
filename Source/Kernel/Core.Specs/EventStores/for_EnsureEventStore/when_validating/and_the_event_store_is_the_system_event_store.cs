// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_validating;

public class and_the_event_store_is_the_system_event_store : Specification
{
    readonly CommandScenario<EnsureEventStore> _scenario = ChronicleCommandScenario.For<EnsureEventStore>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new EnsureEventStore("System"));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_name_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("name");
    [Fact] void should_report_the_reserved_store_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The System event store is reserved for the kernel and cannot be created through the public API.");
}
