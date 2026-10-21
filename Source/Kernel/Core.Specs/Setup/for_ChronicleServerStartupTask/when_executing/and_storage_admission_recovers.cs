// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

using Concepts = Cratis.Chronicle.Concepts;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_storage_admission_recovers : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _reactors.DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default).Returns(
        Task.FromException(new MongoWaitQueueFullException("Planted storage admission overload")),
        Task.CompletedTask);

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_retry_the_required_registration() => _reactors.Received(2).DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
    [Fact] void should_complete_after_storage_recovers() => _failure.ShouldBeNull();
    [Fact] void should_finish_required_bootstrap() => _bootstrapClientsEnsured.ShouldBeTrue();
}
