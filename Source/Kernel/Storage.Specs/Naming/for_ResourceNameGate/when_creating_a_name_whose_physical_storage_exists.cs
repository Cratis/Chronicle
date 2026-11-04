// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_a_name_whose_physical_storage_exists : given.a_gate
{
    IResourcePhysicalNames _physical;
    ResourceNameConflict _conflict;
    Exception _free;
    IReadOnlyCollection<string> _keys;

    void Establish()
    {
        _physical = Substitute.For<IResourcePhysicalNames>();
        _physical.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(ci => [ci.ArgAt<EventStoreName>(0).Value + "+es"]);

        // The provider decides equality; here it treats "x+es" as taken (for instance by an accent- or case-insensitive collation).
        _physical.AnyExists(Arg.Any<IReadOnlyCollection<string>>()).Returns(ci => ci.Arg<IReadOnlyCollection<string>>().Contains("x+es"));
        _reservations.Reserve(Arg.Any<string>(), Arg.Do<IReadOnlyCollection<string>>(_ => _keys = _)).Returns(true);
        _gate = new(_snapshot, _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance, _physical);
    }

    void Because()
    {
        _conflict = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Store("x")).GetAwaiter().GetResult());
        _free = Catch.Exception(() => _gate.EnsureCanCreate(Store("y")).GetAwaiter().GetResult());
    }

    [Fact] void should_refuse_with_an_overlap() => _conflict.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_allow_a_free_name() => _free.ShouldBeNull();
    [Fact] void should_ask_the_provider_with_the_exact_physical_names() => _physical.Received(1).AnyExists(Arg.Is<IReadOnlyCollection<string>>(_ => _.SequenceEqual(new[] { "y+es" })));
    [Fact] void should_reserve_the_folded_physical_name() => _keys.ShouldContain("res:Y+ES");
}
