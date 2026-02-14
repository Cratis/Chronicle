// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Grains.Clients;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class ConnectionService(
    IGrainFactory grainFactory,
    ILogger<ConnectionService> logger) : IConnectionService
{
    static readonly Lazy<string> _schemaDefinition = new(GenerateSchema);

    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        var subject = new Subject<ConnectionKeepAlive>();
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);

        _ = Task.Run(
            async () =>
            {
                await connectedClients.OnClientConnected(
                    request.ConnectionId,
                    request.ClientVersion,
                    request.IsRunningWithDebugger);

                try
                {
                    while (!context.CancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        subject.OnNext(new ConnectionKeepAlive
                        {
                            ConnectionId = request.ConnectionId
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.FailureDuringKeepAlive(request.ConnectionId, ex);
                }

                await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
            },
            context.CancellationToken);

        context.CancellationToken.Register(() =>
        {
            subject.OnCompleted();
            subject.Dispose();
        });

        return subject;
    }

    /// <inheritdoc/>
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.OnClientPing(keepAlive.ConnectionId);
    }

    /// <inheritdoc/>
    public Task<DescriptorSetResponse> GetDescriptorSet()
    {
        return Task.FromResult(new DescriptorSetResponse
        {
            SchemaDefinition = _schemaDefinition.Value
        });
    }

    static string GenerateSchema()
    {
        var generator = new SchemaGenerator
        {
            ProtoSyntax = ProtoSyntax.Proto3
        };

        // Add all service types that are registered
        var serviceTypes = new[]
        {
            typeof(Contracts.IEventStores),
            typeof(Contracts.INamespaces),
            typeof(Contracts.Recommendations.IRecommendations),
            typeof(Contracts.Identities.IIdentities),
            typeof(Contracts.EventSequences.IEventSequences),
            typeof(Contracts.Events.IEventTypes),
            typeof(Contracts.Events.Constraints.IConstraints),
            typeof(Contracts.Clients.IConnectionService),
            typeof(Contracts.Observation.IObservers),
            typeof(Contracts.Observation.IFailedPartitions),
            typeof(Contracts.Observation.Reactors.IReactors),
            typeof(Contracts.Observation.Reducers.IReducers),
            typeof(Contracts.Observation.Webhooks.IWebhooks),
            typeof(Contracts.Projections.IProjections),
            typeof(Contracts.ReadModels.IReadModels),
            typeof(Contracts.Jobs.IJobs),
            typeof(Contracts.Seeding.IEventSeeding),
            typeof(Contracts.Security.IUsers),
            typeof(Contracts.Security.IApplications),
            typeof(Contracts.Host.IServer)
        };

        return generator.GetSchema(serviceTypes);
    }
}
