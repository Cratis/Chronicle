// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class and_the_storage_was_removed : given.a_gate
{
    Exception _result;

    void Establish()
    {
        var physical = Substitute.For<IResourcePhysicalNames>();
        physical.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(["shop+tenant"]);
        physical.AnyExists(Arg.Any<IReadOnlyCollection<string>>()).Returns(false);
        _gate = new(_snapshot, _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance, physical);
    }

    void Because() => _result = Catch.Exception(() => _gate.EnsureCanCreate(Namespace("shop", "tenant", [])).GetAwaiter().GetResult());

    [Fact] void should_allow_it() => _result.ShouldBeNull();
    [Fact] void should_reserve_it() => _reservations.Received(1).Reserve("shop#tenant", Arg.Any<IReadOnlyCollection<string>>());
}
