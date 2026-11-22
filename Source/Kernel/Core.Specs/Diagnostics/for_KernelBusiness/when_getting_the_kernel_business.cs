// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.Load;
using Cratis.Chronicle.Observation.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.for_KernelBusiness;

public class when_getting_the_kernel_business : Specification
{
    ManualTimeProvider _time;
    KernelLoad _load;
    ObserverJobGauges _jobs;
    ILocalSiloDetails _silo;
    KernelBusiness _business;

    void Establish()
    {
        _time = new();
        _load = new KernelLoad(Options.Create(new ChronicleOptions()), NullLogger<KernelLoad>.Instance, null, _time);
        _jobs = new ObserverJobGauges(Substitute.For<Cratis.Metrics.IMeter<ObserverJobGauges>>());
        _silo = Substitute.For<ILocalSiloDetails>();
        _silo.SiloAddress.Returns(SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11111), 1));

        _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(20));
        _load.StreamOpened();
        var key = new ObserverKey("the-observer", "the-store", "the-namespace", "event-log");
        _jobs.Register(Guid.NewGuid(), key, ObserverJobGaugeType.CatchUp);
        _jobs.Register(Guid.NewGuid(), key, ObserverJobGaugeType.CatchUp);
    }

    async Task Because() => _business = await KernelBusiness.GetKernelBusiness(_load, _jobs, _silo);

    [Fact] void should_report_the_status() => _business.Status.ShouldEqual(KernelLoadStatus.Degraded);
    [Fact] void should_say_why() => _business.Reasons.Single().ShouldContain("IProjectionsManager");
    [Fact] void should_list_the_grain_with_the_oldest_queued_call() => _business.BusiestGrains.Single().OldestPendingAgeSeconds.ShouldEqual(20);
    [Fact] void should_report_the_open_streams() => _business.OpenStreams.ShouldEqual(1);
    [Fact] void should_count_the_running_jobs_by_type() => _business.RunningJobs.Single().JobType.ShouldEqual("catch_up");
    [Fact] void should_count_how_many_jobs_run() => _business.RunningJobs.Single().Count.ShouldEqual(2);
    [Fact] void should_name_the_event_store_of_the_jobs() => _business.RunningJobs.Single().EventStore.ShouldEqual("the-store");
}
