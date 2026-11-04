// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_a_case_variant_of_an_existing_namespace : given.a_namespaces_grain
{
    Exception _error;

    void Establish()
    {
        _state.Namespaces.Add(new NamespaceState("FUTURE", DateTimeOffset.UtcNow));
        var reservations = Substitute.For<IResourceNameReservations>();
        reservations.Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(true);
        _silo.AddService<IResourceNameGate>(new ResourceNameGate(new ResourceNameSnapshot(), reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance));
    }

    async Task Because() => _error = await Catch.Exception(() => _namespaces.Ensure(_namespace));

    [Fact] void should_refuse_it_as_a_case_variant() => ((ResourceNameConflict)_error).Kind.ShouldEqual(ResourceNameConflictKind.NamespaceCaseVariant);
    [Fact] async Task should_not_append_a_namespace_added_event() =>
        await _systemSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
    [Fact] void should_not_add_the_variant() => _state.NewNamespaces.ShouldBeEmpty();
}
