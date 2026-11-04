// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.given;

/// <summary>
/// An application host that starts Arc and its mapped endpoints without opening a network listener.
/// </summary>
public abstract class a_startable_application_host : an_application_host
{
    protected Exception? _startupFailure;
    protected IEnumerable<ICommandHandler> _servedCommandHandlers;
    protected IEnumerable<IQueryPerformer> _servedQueryPerformers;
    protected IEnumerable<bool> _kernelCommandLookups;
    protected IEnumerable<bool> _kernelQueryLookups;
    protected bool _applicationCommandFound;
    protected bool _applicationQueryFound;

    protected abstract bool ChronicleBeforeArc { get; }

    void Establish() => BuildHost(ChronicleBeforeArc);

    protected async Task StartHost()
    {
        _startupFailure = await Catch.Exception(() => _application.StartAsync());
        if (_startupFailure is null)
        {
            await _application.StopAsync();
        }

        var commandHandlers = _application.Services.GetRequiredService<ICommandHandlerProviders>();
        var queryPerformers = _application.Services.GetRequiredService<IQueryPerformerProviders>();
        _servedCommandHandlers = commandHandlers.Handlers.ToArray();
        _servedQueryPerformers = queryPerformers.Performers.ToArray();
        _applicationCommandFound = commandHandlers.TryGetHandlerFor(new ApplicationCommand(), out var _);
        _applicationQueryFound = queryPerformers.TryGetPerformersFor(_allQueryPerformers.Single(_ => _.Type == typeof(ApplicationQuery)).FullyQualifiedName, out var _);
        _kernelCommandLookups = KernelCommandHandlersIn(_allCommandHandlers)
            .Select(_ => commandHandlers.TryGetHandlerFor(RuntimeHelpers.GetUninitializedObject(_.CommandType), out var _))
            .ToArray();
        _kernelQueryLookups = KernelQueryPerformersIn(_allQueryPerformers)
            .Select(_ => queryPerformers.TryGetPerformersFor(_.FullyQualifiedName, out var _))
            .ToArray();
    }
}
