// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

using Concepts = Cratis.Chronicle.Concepts;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_an_unrelated_foreign_failure_is_not_transient : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _reactors.DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default).Returns(
        _ => FailThroughIncomingCallFilter(new Foreign.Driver.PoolExhaustedException("pool is full")));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_propagate_a_serializable_failure() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_the_original_diagnostics() => _failure!.Message.ShouldEqual("Foreign.Driver.PoolExhaustedException: pool is full");
    [Fact] void should_not_keep_a_foreign_inner_exception() => _failure!.InnerException.ShouldBeNull();
    [Fact] void should_not_classify_the_foreign_failure_as_transient() => SiblingSiloInstability.IsTransient(_failure!).ShouldBeFalse();
    [Fact] void should_not_retry_the_required_registration() => _reactors.Received(1).DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
    [Fact] void should_not_report_bootstrap_complete() => _bootstrapClientsEnsured.ShouldBeFalse();
}
