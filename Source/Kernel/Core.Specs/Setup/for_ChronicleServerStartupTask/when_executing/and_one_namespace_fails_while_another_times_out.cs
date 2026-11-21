// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// The namespaces of an event store are rehydrated in the background independently of each other: a namespace that
/// keeps failing, or timing out against a busy sibling, neither stops the others nor fails the host.
/// </summary>
public class and_one_namespace_fails_while_another_times_out : given.a_startup_task
{
    EventStoreNamespaceName _timingOutNamespace;
    IJobsManager _timingOutJobsManager;
    Exception _error;

    void Establish()
    {
        _timingOutNamespace = "timing-out";
        _timingOutJobsManager = Substitute.For<IJobsManager>();
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _timingOutNamespace)).Returns(_timingOutJobsManager);
        _eventStoreStorage.GetNamespace(_timingOutNamespace).Returns(_namespaceStorage);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace, _timingOutNamespace]));

        _timingOutJobsManager.Rehydrate().Returns(_ => Task.FromException(new TimeoutException("Response did not arrive on time")));
        _jobsManager.Rehydrate().Returns(_ => Task.FromException(new InvalidOperationException("A real failure in one namespace")));
    }

    async Task Because() => _error = await Catch.Exception(() => Execute().WaitAsync(TimeSpan.FromSeconds(30)));

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] void should_complete_startup() => _bootstrapClientsEnsured.ShouldBeTrue();
    [Fact] async Task should_retry_the_failing_namespace_every_pass() => await _jobsManager.Received(6).Rehydrate();
    [Fact] async Task should_retry_the_timing_out_namespace_every_pass() => await _timingOutJobsManager.Received(6).Rehydrate();
    [Fact] void should_back_off_between_passes() => RequestedDelays.Take(3).ShouldContainOnly([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20)]);
    [Fact] void should_report_giving_up() => LogEntries.Count(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkGaveUp)).ShouldEqual(1);
}
