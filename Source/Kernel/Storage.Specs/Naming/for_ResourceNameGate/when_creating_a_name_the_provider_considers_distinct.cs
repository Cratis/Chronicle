// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

/// <summary>
/// A provider with a case-sensitive collation (PostgreSQL "C") holds "X+ES"; "x+es" is a different database there,
/// so the gate must not fold the names itself and refuse.
/// </summary>
public class when_creating_a_name_the_provider_considers_distinct : given.a_gate
{
    Exception _error;

    void Establish()
    {
        var physical = Substitute.For<IResourcePhysicalNames>();
        physical.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(ci => [ci.ArgAt<EventStoreName>(0).Value + "+es"]);
        physical.AnyExists(Arg.Any<IReadOnlyCollection<string>>()).Returns(ci => ci.Arg<IReadOnlyCollection<string>>().Contains("X+ES"));
        _gate = new(_snapshot, _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance, physical);
    }

    void Because() => _error = Catch.Exception(() => _gate.EnsureCanCreate(Store("x")).GetAwaiter().GetResult());

    [Fact] void should_allow_it() => _error.ShouldBeNull();
}
