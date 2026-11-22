// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReactorSubscriptions"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the observer and the connected clients.</param>
/// <param name="reactorDefinitionComparer"><see cref="IReactorDefinitionComparer"/> for comparing reactor definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo the stream is on.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for accessing Chronicle configuration.</param>
/// <param name="storage">The <see cref="IStorage"/> holding the shared reactor definitions.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for creating loggers.</param>
public class ClientReactorSubscriptions(
    IGrainFactory grainFactory,
    IReactorDefinitionComparer reactorDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    IOptions<ChronicleOptions> options,
    IStorage storage,
    ILoggerFactory loggerFactory) : IClientReactorSubscriptions
{
    /// <inheritdoc/>
    public IClientReactorSubscription Create(ConnectedObserverKey key) =>
        new ClientReactorSubscription(
            key,
            grainFactory,
            reactorDefinitionComparer,
            localSiloDetails,
            options,
            storage,
            loggerFactory.CreateLogger<ClientReactorSubscription>());
}
