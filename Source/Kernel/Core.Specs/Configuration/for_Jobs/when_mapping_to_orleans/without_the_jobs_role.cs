// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_Jobs.when_mapping_to_orleans;

public class without_the_jobs_role : Specification
{
    IHost _host;
    Cratis.Orleans.Jobs.JobsOptions _options;

    void Establish() => _host = new HostBuilder()
        .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
        .UseOrleans(silo =>
        {
            silo.AddChronicleToSilo();
            silo.Services.AddSingleton(Options.Create(new ChronicleOptions
            {
                Clustering = new() { Roles = new() { Jobs = false } },
                Jobs = new() { MaxParallelSteps = 20 }
            }));
        }).Build();

    void Because() => _options = _host.Services.GetRequiredService<IOptions<Cratis.Orleans.Jobs.JobsOptions>>().Value;

    void Destroy() => _host?.Dispose();

    [Fact] void should_map_the_role_budget() => _options.MaxParallelSteps.ShouldEqual(5);
    [Fact] void should_preserve_the_dead_job_threshold() => _options.DeadJobThreshold.ShouldEqual(TimeSpan.FromHours(1));
    [Fact] void should_preserve_the_cleanup_cadence() => _options.CleanupCadence.ShouldEqual(TimeSpan.FromHours(1));
    [Fact] void should_preserve_the_checkpoint_batch_interval() => _options.StepCheckpointBatchInterval.ShouldEqual(100);
    [Fact] void should_preserve_the_checkpoint_flush_interval() => _options.StepCheckpointFlushInterval.ShouldEqual(TimeSpan.FromSeconds(5));
}
