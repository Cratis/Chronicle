// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Namespaces start in parallel. When one fails for real, the others are cancelled and the failure reaches the host -
/// even when another namespace is waiting for a busy silo, which on its own would wait far longer.
/// </summary>
public class and_one_namespace_fails_while_another_waits_for_a_busy_silo : given.a_startup_task
{
    readonly TaskCompletionSource _waitingForBusySilo = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly InvalidOperationException _defect = new("A real failure in one namespace");
    EventStoreNamespaceName _busyNamespace;
    IJobsManager _busyJobsManager;
    Exception _error;
    int _busyCalls;

    protected override ChronicleOptions ServerOptions => new() { Startup = new() { BusySiloWaitLimit = TimeSpan.FromDays(3650) } };

    void Establish()
    {
        _busyNamespace = "busy";
        _busyJobsManager = Substitute.For<IJobsManager>();
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _busyNamespace)).Returns(_busyJobsManager);
        _eventStoreStorage.GetNamespace(_busyNamespace).Returns(_namespaceStorage);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace, _busyNamespace]));

        _busySilos.FindBusySilo(Arg.Is<Exception>(_ => _ is TimeoutException)).Returns(new BusySilo("10.0.0.1:11111:42", true));
        _busyJobsManager.Rehydrate().Returns(_ =>
        {
            Interlocked.Increment(ref _busyCalls);
            _waitingForBusySilo.TrySetResult();
            return Task.FromException(new TimeoutException("Response did not arrive on time"));
        });
        _jobsManager.Rehydrate().Returns(async _ =>
        {
            await _waitingForBusySilo.Task;
            throw _defect;
        });
    }

    async Task Because() => _error = await Catch.Exception(() => Execute().WaitAsync(TimeSpan.FromSeconds(30)));

    [Fact] void should_fail_the_host_with_the_real_failure() => _error.ShouldEqual(_defect);
    [Fact] void should_have_been_waiting_for_the_busy_silo() => _busyCalls.ShouldBeGreaterThan(0);
}
