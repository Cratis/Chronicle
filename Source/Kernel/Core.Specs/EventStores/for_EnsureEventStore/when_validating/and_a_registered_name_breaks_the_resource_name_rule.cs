// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_validating;

public class and_a_registered_name_breaks_the_resource_name_rule : Specification
{
    readonly CommandScenario<EnsureEventStore> _scenario = ChronicleCommandScenario.For<EnsureEventStore>();
    CommandResult _registered;
    CommandResult _caseVariant;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(ci => ci.Arg<EventStoreName>().Value == "legacy$store");
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because()
    {
        _registered = await _scenario.Validate(new EnsureEventStore("legacy$store"));
        _caseVariant = await _scenario.Validate(new EnsureEventStore("LEGACY$STORE"));
    }

    [Fact] void should_allow_the_registered_owner_to_reconnect() => _registered.ShouldBeValid();
    [Fact] void should_refuse_a_new_case_variant() => _caseVariant.IsSuccess.ShouldBeFalse();
}
