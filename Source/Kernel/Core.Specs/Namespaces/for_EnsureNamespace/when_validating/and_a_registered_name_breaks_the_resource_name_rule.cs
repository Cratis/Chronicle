// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_validating;

public class and_a_registered_name_breaks_the_resource_name_rule : Specification
{
    readonly CommandScenario<EnsureNamespace> _scenario = ChronicleCommandScenario.For<EnsureNamespace>();
    CommandResult _registered;
    CommandResult _caseVariant;
    CommandResult _otherStore;
    CommandResult _combination;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(["legacy$tenant"]));
        var none = Substitute.For<INamespaces>();
        none.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([]));
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0) == "some-event-store" ? namespaces : none);
        _scenario.Services.AddSingleton(grainFactory);
    }

    async Task Because()
    {
        _registered = await _scenario.Validate(new EnsureNamespace("some-event-store", "legacy$tenant"));
        _caseVariant = await _scenario.Validate(new EnsureNamespace("some-event-store", "LEGACY$TENANT"));
        _otherStore = await _scenario.Validate(new EnsureNamespace("other-event-store", "legacy$tenant"));
        _combination = await _scenario.Validate(new EnsureNamespace("Chronicle", "MAIN"));
    }

    [Fact] void should_allow_the_registered_owner_to_reconnect() => _registered.ShouldBeValid();
    [Fact] void should_refuse_a_new_case_variant() => _caseVariant.IsSuccess.ShouldBeFalse();
    [Fact] void should_refuse_the_name_in_a_store_where_it_is_not_registered() => _otherStore.IsSuccess.ShouldBeFalse();
    [Fact] void should_never_grandfather_the_cluster_combination() => _combination.IsSuccess.ShouldBeFalse();
}
