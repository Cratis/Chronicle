// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_recreating_a_forgotten_namespace_whose_storage_remains : given.a_gate
{
    ResourceNameConflict _conflict;

    void Establish()
    {
        var physical = Substitute.For<IResourcePhysicalNames>();
        physical.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(["shop+tenant"]);
        physical.AnyExists(Arg.Any<IReadOnlyCollection<string>>()).Returns(true);
        _gate = new(_snapshot, _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance, physical);
    }

    void Because()
    {
        // The namespace is no longer registered, so its name is not in the registered names.
        _conflict = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Namespace("shop", "tenant", [])).GetAwaiter().GetResult());
    }

    [Fact] void should_refuse_it() => _conflict.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_not_write_a_reservation() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
