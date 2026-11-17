// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Traces;
using Microsoft.Extensions.Options;
using KernelFailedPartitionsService = KernelGrpc::Cratis.Chronicle.Services.Observation.FailedPartitions;
using KernelObserverRemover = KernelCore::Cratis.Chronicle.Observation.ObserverRemover;
using KernelObserversService = KernelGrpc::Cratis.Chronicle.Services.Observation.Observers;
using KernelProjectionsService = KernelGrpc::Cratis.Chronicle.Services.Projections.Projections;
using KernelReactorMediator = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients.ReactorMediator;
using KernelReactorsService = KernelGrpc::Cratis.Chronicle.Services.Observation.Reactors.Reactors;
using KernelReducerMediator = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.ReducerMediator;
using KernelReducersService = KernelGrpc::Cratis.Chronicle.Services.Observation.Reducers.Reducers;
using KernelSubscriptionsService = KernelGrpc::Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions;
using KernelWebhooksService = KernelGrpc::Cratis.Chronicle.Services.Observation.Webhooks.Webhooks;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Creates the observation related kernel gRPC services backed by in-memory storage.
/// </summary>
// The factories deliberately return the contract interface the lazily resolved services are exposed as.
#pragma warning disable CA1859
internal sealed partial class TestingServices
{
    static IObservers CreateObservers(TestingServiceContext context) =>
        new ObserversForTesting(new KernelObserversService(
            context.GrainFactory,
            context.Storage,
            new KernelObserverRemover(context.GrainFactory, context.Storage, NullLogger<KernelObserverRemover>.Instance),
            new KernelCore::Cratis.Chronicle.Clients.ConnectedClientsVisibility(context.KernelOptions, context.ConnectionOwnership, new Microsoft.AspNetCore.Http.HttpContextAccessor()),
            context.KernelOptions,
            context.NamespaceCasing));

    static IFailedPartitions CreateFailedPartitions(TestingServiceContext context) =>
        new KernelFailedPartitionsService(context.Storage);

    static IReactors CreateReactors(TestingServiceContext context) =>
        new KernelReactorsService(
            context.GrainFactory,
            new KernelReactorMediator(),
            context.Storage,
            context.JsonSerializerOptions,
            new ActivitySource<KernelReactorsService>(),
            Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()),
            context.ConnectionOwnership,
            NullLogger<KernelReactorsService>.Instance,
            context.RegistrationGate,
            context.NamespaceCasing,
            KernelGrpc::Cratis.Chronicle.Services.Observation.NoSiloDepartures.Instance);

    static IReducers CreateReducers(TestingServiceContext context) =>
        new KernelReducersService(
            context.GrainFactory,
            new KernelReducerMediator(),
            new ExpandoObjectConverter(new TypeFormats()),
            context.JsonSerializerOptions,
            new ActivitySource<KernelReducersService>(),
            Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()),
            context.ConnectionOwnership,
            NullLogger<KernelReducersService>.Instance,
            context.RegistrationGate,
            context.NamespaceCasing,
            KernelGrpc::Cratis.Chronicle.Services.Observation.NoSiloDepartures.Instance);

    static IProjections CreateProjections(TestingServiceContext context) =>
        new KernelProjectionsService(
            context.GrainFactory,
            new ExpandoObjectConverter(new TypeFormats()),
            null!,
            null!,
            null!,
            null!,
            context.ConnectionOwnership,
            context.KernelOptions,
            NullLogger<KernelProjectionsService>.Instance,
            context.NamespaceCasing);

    static IWebhooks CreateWebhooks(TestingServiceContext context) =>
        new KernelWebhooksService(
            context.CommandPipeline.Value,
            context.Storage,
            NullLogger<KernelWebhooksService>.Instance);

    static IEventStoreSubscriptions CreateEventStoreSubscriptions(TestingServiceContext context) =>
        new KernelSubscriptionsService(context.GrainFactory, context.Storage, Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()));
}
#pragma warning restore CA1859
