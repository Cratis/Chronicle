// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ArcCommandResult = Cratis.Arc.Commands.CommandResult;
using KernelSecurity = KernelConcepts::Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.given;

/// <summary>
/// An application host that also runs the kernel in its own container, composed the way the in-process integration
/// fixtures compose one: Arc, the Chronicle client and an Orleans silo with Chronicle added, all on one builder.
/// The silo reuses the host's Arc command pipeline, so the kernel dispatches its commands through this container.
/// </summary>
public abstract class an_application_host_with_the_kernel : Specification, IDisposable
{
    protected static readonly Assembly _kernelCoreAssembly = typeof(KernelCore::Cratis.Chronicle.Jobs.DeleteJob).Assembly;

    protected WebApplication _application;
    protected int _kernelCommandsInTheUniverse;
    protected int _kernelQueriesInTheUniverse;
    protected IEnumerable<ICommandHandler> _servedKernelCommands;
    protected IEnumerable<IQueryPerformer> _servedKernelQueries;
    protected ArcCommandResult _kernelCommandResult;
    protected bool _kernelCommandHandlerFound;

    protected abstract bool KernelBeforeClient { get; }

    public void Dispose() => (_application as IDisposable)?.Dispose();

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new ChronicleTelemetryConfiguration(_ => { }));
        builder.AddCratisArc();
        builder.Host.UseDefaultServiceProvider(_ => _.ValidateOnBuild = false);

        // The in-process pipeline specs contribute a command execution scope to the shared type universe.
        builder.Services.AddSingleton(Substitute.For<for_InProcessCommandPipeline.IConsumerScope>());
        if (KernelBeforeClient)
        {
            AddKernel(builder);
            builder.AddCratisChronicle();
        }
        else
        {
            builder.AddCratisChronicle();
            AddKernel(builder);
        }

        _application = builder.Build();

        var types = _application.Services.GetRequiredService<ITypes>();
        _kernelCommandsInTheUniverse = new CommandHandlerProviders(new InstancesOf<ICommandHandlerProvider>(types, _application.Services)).Handlers
            .Count(_ => _.CommandType.Assembly == _kernelCoreAssembly);
        _kernelQueriesInTheUniverse = new QueryPerformerProviders(new InstancesOf<IQueryPerformerProvider>(types, _application.Services)).Performers
            .Count(_ => _.Type.Assembly == _kernelCoreAssembly);
    }

    protected async Task DispatchAKernelCommand()
    {
        _servedKernelCommands = _application.Services.GetRequiredService<ICommandHandlerProviders>().Handlers
            .Where(_ => _.CommandType.Assembly == _kernelCoreAssembly)
            .ToArray();
        _servedKernelQueries = _application.Services.GetRequiredService<IQueryPerformerProviders>().Performers
            .Where(_ => _.Type.Assembly == _kernelCoreAssembly)
            .ToArray();

        var command = new KernelCore::Cratis.Chronicle.Security.SetInitialAdminPassword(
            new KernelSecurity.UserId(Guid.NewGuid()),
            new KernelSecurity.Password("a-test-password"),
            new KernelSecurity.Password("a-different-test-password"),
            new KernelSecurity.InitialAdminSetupToken("a-setup-token"));
        _kernelCommandHandlerFound = _application.Services.GetRequiredService<ICommandHandlerProviders>().TryGetHandlerFor(command, out _);

        await using var scope = _application.Services.CreateAsyncScope();
        _kernelCommandResult = await scope.ServiceProvider.GetRequiredService<ICommandPipeline>().Execute(command, scope.ServiceProvider);
    }

    static void AddKernel(WebApplicationBuilder builder) =>
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering();
            KernelGrpc::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(silo);
        });
}
