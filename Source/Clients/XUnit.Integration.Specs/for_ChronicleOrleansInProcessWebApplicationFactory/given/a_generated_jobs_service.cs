// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

using System.Collections.Immutable;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using CommandResult = Cratis.Chronicle.Contracts.Commands.CommandResult;
using KernelChronicleOptions = KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory.given;

public class a_generated_jobs_service : Specification
{
    protected IJobsManager _jobsManager;
    protected JobId _jobId;
    protected CommandResult _result;
    protected bool _systemEnforced;
    IGrainFactory _grainFactory;
    IStorage _storage;
    a_build_only_factory _factory;
    IHost _host;
    ICommandPipeline _pipeline;
    IAuthorizationPolicyRuntime _runtime;

    void Establish()
    {
        _jobId = JobId.New();
        _jobsManager = Substitute.For<IJobsManager>();
        _jobsManager.GetAllJobs().Returns(new List<JobState> { new() { Id = _jobId } }.ToImmutableList());
        _jobsManager.Resume(_jobId).Returns(true);
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(_jobsManager);
        _storage = Substitute.For<IStorage>();
        _storage.HasEventStore(Arg.Any<global::Cratis.Chronicle.Concepts.EventStoreName>()).Returns(true);
    }

    protected async Task Resume(string eventStore, bool enforceSystem = false, bool allowAnonymousPolicy = true)
    {
        _factory = new(configuration: new Dictionary<string, string?>
        {
            [$"{KernelChronicleOptions.SectionPath}:Authentication:Enabled"] = "true",
            [$"{KernelChronicleOptions.SectionPath}:Authentication:EnforceAdministratorPolicyForOperations"] = enforceSystem.ToString()
        });
        _host = _factory.BuildHost(services =>
        {
            services
                .AddSingleton(_grainFactory)
                .AddSingleton(_storage);
            if (!allowAnonymousPolicy)
            {
                // Keep the kernel's real requirements and handlers; remove only Arc's anonymous opt-ins.
                services.RemoveAll<AnonymousAspNetAuthorizationPolicyRegistration>();
            }
        });
        _pipeline = _host.Services.GetRequiredService<ICommandPipeline>();
        _runtime = _host.Services.GetRequiredService<IAuthorizationPolicyRuntime>();
        _systemEnforced = _host.Services.GetRequiredService<IOptions<KernelChronicleOptions>>()
            .Value.Authentication.EnforceAdministratorPolicyForOperations;
        var serviceType = typeof(KernelGrpc::Orleans.Hosting.ChronicleServerSiloBuilderExtensions).Assembly
            .GetType("Cratis.Chronicle.Services.Jobs.Jobs", throwOnError: true)!;
        var jobs = (global::Cratis.Chronicle.Contracts.Jobs.IJobs)ActivatorUtilities.CreateInstance(_host.Services, serviceType);
        using var principal = _host.Services.GetRequiredService<ICurrentPrincipalOverride>()
            .BeginScope(new ClaimsPrincipal(new ClaimsIdentity()));
        _result = await jobs.ResumeJob(new ResumeJobRequest
        {
            EventStore = eventStore,
            Namespace = "some-namespace",
            JobId = _jobId.Value
        });
    }

    protected void ShouldUseTheRealArcPipeline() => _pipeline.ShouldBeOfExactType<CommandPipeline>();

    protected void ShouldUseTheAspNetPolicyRuntime() => _runtime.ShouldBeOfExactType<AspNetAuthorizationPolicyRuntime>();

    protected void ShouldKeepAuthenticationEnabled() => _host.Services.GetRequiredService<IOptions<KernelChronicleOptions>>()
        .Value.Authentication.Enabled.ShouldBeTrue();

    void Destroy()
    {
        _host?.Dispose();
        _factory?.Dispose();
    }
}
