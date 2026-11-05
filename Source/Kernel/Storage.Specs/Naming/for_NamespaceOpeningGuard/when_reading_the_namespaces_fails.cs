// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Naming.for_NamespaceOpeningGuard;

public class when_reading_the_namespaces_fails : given.a_guard_on_a_single_threaded_scheduler
{
    Exception? _result;
    Exception? _again;

    void Establish() => _namespaces.GetAll().Returns(_ => Task.FromException<IEnumerable<NamespaceState>>(new StorageUnreachable()));

    async Task Because()
    {
        _result = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));
        _again = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));
    }

    [Fact] void should_refuse_the_open() => _result.ShouldBeOfExactType<StorageUnreachable>();
    [Fact] void should_check_again_on_the_next_open() => _again.ShouldBeOfExactType<StorageUnreachable>();
    [Fact] void should_not_check_the_creation() => _gate.DidNotReceive().EnsureCanCreate(Arg.Any<ResourceCreation>());

    sealed class StorageUnreachable : Exception;
}
