// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_removing_then_recreating_a_namespace : given.a_namespace_remover
{
    readonly List<string> _registered = [_namespace.Value];
    ResourceNameGate _gate;
    IResourceNameReservations _reservations;
    Exception _error;

    void Establish()
    {
        // Forgetting leaves the registered names without the namespace, as the namespaces grain does.
        _namespaces.Forget(_namespace).Returns(_ =>
        {
            _registered.Remove(_namespace.Value);
            return Task.CompletedTask;
        });

        // The read model storage of the namespace is still there after the removal.
        var physical = Substitute.For<IResourcePhysicalNames>();
        physical.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(["some-store+tenant"]);
        physical.AnyExists(Arg.Any<IReadOnlyCollection<string>>()).Returns(true);

        var snapshot = ResourceNameSnapshot.NotReady();
        snapshot.Publish([], []);
        _reservations = Substitute.For<IResourceNameReservations>();
        _reservations.Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(true);
        _gate = new(snapshot, _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance, physical);
    }

    async Task Because()
    {
        await _remover.Remove(_eventStore, _namespace);
        _error = await Catch.Exception(() => _gate.EnsureCanCreate(new ResourceCreation(_eventStore, _namespace, _registered)));
    }

    [Fact] void should_have_forgotten_the_namespace() => _registered.ShouldBeEmpty();
    [Fact] void should_refuse_creating_it_again() => _error.ShouldBeOfExactType<ResourceNameConflict>();
    [Fact] void should_not_write_a_reservation() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
