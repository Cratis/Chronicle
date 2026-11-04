// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using MongoDB.Driver;

using Concepts = Cratis.Chronicle.Concepts;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_storage_admission_never_recovers : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _reactors.DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default).Returns(
        _ => FailThroughIncomingCallFilter(new MongoWaitQueueFullException("Planted storage admission overload")));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_fail_closed_with_the_storage_failure() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_the_storage_admission_category() => _failure!.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");
    [Fact] void should_not_keep_a_driver_inner_exception() => _failure!.InnerException.ShouldBeNull();
    [Fact] void should_preserve_the_transient_storage_failure() => SiblingSiloInstability.IsTransient(_failure!).ShouldBeTrue();
    [Fact] void should_keep_the_original_storage_diagnostics() => _failure!.Message.ShouldEqual("MongoDB.Driver.MongoWaitQueueFullException: Planted storage admission overload");
    [Fact] void should_stop_after_five_attempts() => _reactors.Received(5).DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
    [Fact] void should_not_report_bootstrap_complete() => _bootstrapClientsEnsured.ShouldBeFalse();
}
