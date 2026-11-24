// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Text.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Testing.Compliance;
using Microsoft.Extensions.Options;
using KernelProjectionsManager = KernelCore::Cratis.Chronicle.Projections.Engine.ProjectionsManager;
using KernelReadModelInstanceVisibilities = KernelCore::Cratis.Chronicle.ReadModels.ReadModelInstanceVisibilities;
using KernelReadModelReadBindings = KernelCore::Cratis.Chronicle.ReadModels.ReadModelReadBindings;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents the shared collaborators every in-process kernel service created by <see cref="TestingServices"/> is wired from.
/// </summary>
internal sealed class TestingServiceContext
{
    /// <summary>Gets the <see cref="IGrainFactory"/> for grain-based operations.</summary>
    internal required IGrainFactory GrainFactory { get; init; }

    /// <summary>Gets the <see cref="IStorage"/> backed by in-memory implementations.</summary>
    internal required IStorage Storage { get; init; }

    /// <summary>Gets the scenario's shared compliance stack and encryption keys.</summary>
    internal required InProcessCompliance Compliance { get; init; }

    /// <summary>Gets the <see cref="JsonSerializerOptions"/> for serialization.</summary>
    internal required JsonSerializerOptions JsonSerializerOptions { get; init; }

    /// <summary>Gets the resolver of the event types belonging to this test store.</summary>
    internal required Func<Cratis.Chronicle.Events.IEventTypes> EventTypes { get; init; }

    /// <summary>Gets the optional reply-loss driver.</summary>
    internal DropReplyInterceptor? Replies { get; init; }

    /// <summary>Gets the namespace casing shared by the services.</summary>
    internal InProcessNamespaceCasing NamespaceCasing { get; private set; } = default!;

    /// <summary>Gets the namespace service using the scenario's default-disabled kernel options.</summary>
    internal KernelCore::Cratis.Chronicle.Namespaces.INamespaceIncarnations NamespaceIncarnations { get; private set; } = default!;

    /// <summary>Gets the one command pipeline serving every command-dispatching service.</summary>
    internal Lazy<Arc.Commands.ICommandPipeline> CommandPipeline { get; private set; } = default!;

    /// <summary>Gets the connection ownership shared by the observer-registering services.</summary>
    internal InProcessConnectionOwnership ConnectionOwnership { get; private set; } = default!;

    /// <summary>Gets the kernel options shared by the services that take them once.</summary>
    internal IOptions<KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions> KernelOptions { get; private set; } = default!;

    /// <summary>Gets the observer registration gate shared by the reactor and reducer services.</summary>
    internal UnboundedObserverRegistrationGate RegistrationGate { get; private set; } = default!;

    /// <summary>Gets the read bindings shared by the read model service and its materialized store.</summary>
    internal KernelReadModelReadBindings ReadBindings { get; private set; } = default!;

    /// <summary>
    /// Creates the derived shared collaborators from the supplied inputs.
    /// </summary>
    /// <returns>This <see cref="TestingServiceContext"/>.</returns>
    internal TestingServiceContext Initialize()
    {
        NamespaceCasing = new InProcessNamespaceCasing(Storage);
        CommandPipeline = new(() => TestingCommandPipeline.Create(this));
        ConnectionOwnership = new InProcessConnectionOwnership();
        KernelOptions = Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions());
        NamespaceIncarnations = new KernelCore::Cratis.Chronicle.Namespaces.NamespaceIncarnations(Storage, GrainFactory, KernelOptions);
        RegistrationGate = new UnboundedObserverRegistrationGate();
        ReadBindings = CreateReadBindings();
        return this;
    }

    KernelReadModelReadBindings CreateReadBindings() =>
        new(
            Storage,
            GrainFactory,
            new KernelReadModelInstanceVisibilities(
                new KernelProjectionsManager(Cratis.Chronicle.Testing.ReadModels.ProjectionReadModelProcessor.CreateProjectionFactory(Storage), Storage, GrainFactory)));
}
