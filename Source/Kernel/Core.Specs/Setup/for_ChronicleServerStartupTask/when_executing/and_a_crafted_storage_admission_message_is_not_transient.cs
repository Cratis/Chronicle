// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

using Concepts = Cratis.Chronicle.Concepts;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_a_crafted_storage_admission_message_is_not_transient : given.a_startup_task
{
    readonly InvalidOperationException _original = new("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload");
    Exception? _failure;

    void Establish() => _reactors.DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default).Returns(
        _ => FailThroughIncomingCallFilter(_original));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_propagate_the_original_failure() => _failure.ShouldBeSame(_original);
    [Fact] void should_not_classify_the_message_as_transient() => SiblingSiloInstability.IsTransient(_failure!).ShouldBeFalse();
    [Fact] void should_not_retry_the_required_registration() => _reactors.Received(1).DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
    [Fact] void should_not_report_bootstrap_complete() => _bootstrapClientsEnsured.ShouldBeFalse();
}
