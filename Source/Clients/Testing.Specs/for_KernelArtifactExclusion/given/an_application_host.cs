// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.given;

/// <summary>
/// An application host composed the way a consuming application composes one: Arc and the Chronicle client in one
/// ASP.NET Core host, with Cratis.Chronicle.Testing - and with it the kernel's Core assembly - referenced.
/// </summary>
public class an_application_host : Specification, IDisposable
{
    protected static readonly Assembly _kernelCoreAssembly = Assembly.Load("Cratis.Chronicle.Core");

    protected WebApplication _application;
    protected IEnumerable<ICommandHandler> _allCommandHandlers;
    protected IEnumerable<IQueryPerformer> _allQueryPerformers;

    public void Dispose() => (_application as IDisposable)?.Dispose();

    protected void BuildHost(bool chronicleBeforeArc, Action<IServiceCollection>? configureServices = default, bool registerApplicationPolicy = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new ChronicleTelemetryConfiguration(_ => { }));
        if (chronicleBeforeArc)
        {
            builder.AddCratisChronicle();
            builder.AddCratisArc();
        }
        else
        {
            builder.AddCratisArc();
            builder.AddCratisChronicle();
        }

        if (registerApplicationPolicy)
        {
            builder.Services.AddAuthorizationBuilder().AddPolicy("Application.Required", policy => policy.RequireAuthenticatedUser());
        }

        builder.Services.AddSingleton<IServer>(new NoListenerServer());
        configureServices?.Invoke(builder.Services);
        _application = builder.Build();
        _application.UseCratisArc();

        // What Arc would serve if nothing kept the kernel out: every handler and performer the type universe
        // offers, aggregated exactly as Arc aggregates them. Proves the kernel artifacts are discoverable here,
        // so asserting they are absent from the host's own aggregates is not vacuous.
        var types = _application.Services.GetRequiredService<ITypes>();
        _allCommandHandlers = new CommandHandlerProviders(new InstancesOf<ICommandHandlerProvider>(types, _application.Services)).Handlers;
        _allQueryPerformers = new QueryPerformerProviders(new InstancesOf<IQueryPerformerProvider>(types, _application.Services)).Performers;
    }

    protected IEnumerable<ICommandHandler> KernelCommandHandlersIn(IEnumerable<ICommandHandler> handlers) =>
        handlers.Where(_ => _.CommandType.Assembly == _kernelCoreAssembly);

    protected IEnumerable<IQueryPerformer> KernelQueryPerformersIn(IEnumerable<IQueryPerformer> performers) =>
        performers.Where(_ => _.Type.Assembly == _kernelCoreAssembly);

    sealed class NoListenerServer : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
