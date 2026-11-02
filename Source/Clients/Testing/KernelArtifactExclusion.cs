// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Keeps kernel artifacts out of application hosts that register the Chronicle client.
/// </summary>
/// <remarks>
/// Arc registers its aggregates unconditionally. Decorating their provider collections instead makes the exclusion
/// independent of whether Arc or Chronicle is registered first. The kernel and the private testing pipeline do not
/// register the Chronicle client and retain their original provider collections.
/// </remarks>
public class KernelArtifactExclusion : IChronicleClientServices
{
    static readonly System.Reflection.Assembly _kernelAssembly = typeof(KernelCore::Cratis.Chronicle.Jobs.DeleteJob).Assembly;

    static readonly ServiceDescriptor _commands = ServiceDescriptor.Singleton<IInstancesOf<ICommandHandlerProvider>>(provider =>
        new ProviderInstances<ICommandHandlerProvider>(
            new InstancesOf<ICommandHandlerProvider>(provider.GetRequiredService<ITypes>(), provider)
                .Select(_ => new CommandProvider(_.Handlers.Where(handler => handler.CommandType.Assembly != _kernelAssembly).ToArray()))
                .ToArray()));
    static readonly ServiceDescriptor _queries = ServiceDescriptor.Singleton<IInstancesOf<IQueryPerformerProvider>>(provider =>
        new ProviderInstances<IQueryPerformerProvider>(
            new InstancesOf<IQueryPerformerProvider>(provider.GetRequiredService<ITypes>(), provider)
                .Select(_ => new QueryProvider(_.Performers.Where(performer => performer.Type.Assembly != _kernelAssembly).ToArray()))
                .ToArray()));

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">A custom closed provider collection prevents kernel artifact exclusion.</exception>
    public void Configure(IServiceCollection services)
    {
        RegisterProviderCollection(services, _commands);
        RegisterProviderCollection(services, _queries);
    }

    static void RegisterProviderCollection(IServiceCollection services, ServiceDescriptor descriptor)
    {
        if (services.Any(_ => IsCustomProviderCollection(_, descriptor)))
        {
            throw new InvalidOperationException(
                $"Chronicle.Testing cannot exclude kernel artifacts with a custom '{descriptor.ServiceType}' registration. " +
                "Remove the closed IInstancesOf provider collection registration and register individual ICommandHandlerProvider or IQueryPerformerProvider implementations instead.");
        }

        if (!services.Contains(descriptor))
        {
            services.Add(descriptor);
        }
    }

    static bool IsCustomProviderCollection(ServiceDescriptor registered, ServiceDescriptor expected)
    {
        if (registered.IsKeyedService)
        {
            return false;
        }

        return registered.ServiceType == expected.ServiceType && !ReferenceEquals(registered, expected);
    }

    sealed class CommandProvider(IEnumerable<ICommandHandler> handlers) : ICommandHandlerProvider
    {
        readonly Dictionary<Type, ICommandHandler> _handlers = handlers.DistinctBy(_ => _.CommandType).ToDictionary(_ => _.CommandType);

        public IEnumerable<ICommandHandler> Handlers { get; } = handlers;

        public bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler) =>
            _handlers.TryGetValue(command.GetType(), out handler);
    }

    sealed class QueryProvider(IEnumerable<IQueryPerformer> performers) : IQueryPerformerProvider
    {
        readonly Dictionary<FullyQualifiedQueryName, IQueryPerformer> _performers = performers.DistinctBy(_ => _.FullyQualifiedName).ToDictionary(_ => _.FullyQualifiedName);

        public IEnumerable<IQueryPerformer> Performers { get; } = performers;

        public bool TryGetPerformerFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer) =>
            _performers.TryGetValue(query, out performer);
    }

    sealed class ProviderInstances<T>(IEnumerable<T> providers) : IInstancesOf<T>
        where T : class
    {
        public IEnumerator<T> GetEnumerator() => providers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
