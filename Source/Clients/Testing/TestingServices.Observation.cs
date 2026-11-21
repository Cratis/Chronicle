// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

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
internal sealed partial class TestingServices
{
    static ObserversForTesting CreateObservers(TestingServiceContext context) =>
        new(new KernelObserversService(
            context.GrainFactory,
            context.Storage,
            new KernelObserverRemover(context.GrainFactory, context.Storage, NullLogger<KernelObserverRemover>.Instance),
            new KernelCore::Cratis.Chronicle.Clients.ConnectedClientsVisibility(context.KernelOptions, context.ConnectionOwnership, new Microsoft.AspNetCore.Http.HttpContextAccessor()),
            context.KernelOptions,
            context.NamespaceCasing));

    static KernelFailedPartitionsService CreateFailedPartitions(TestingServiceContext context) =>
        new(context.Storage);

    static KernelReactorsService CreateReactors(TestingServiceContext context) =>
        new(
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
            KernelCore::Cratis.Chronicle.Services.Observation.NoSiloDepartures.Instance);

    static KernelReducersService CreateReducers(TestingServiceContext context) =>
        new(
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
            KernelCore::Cratis.Chronicle.Services.Observation.NoSiloDepartures.Instance);

    static KernelProjectionsService CreateProjections(TestingServiceContext context) =>
        new(
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

    static KernelWebhooksService CreateWebhooks(TestingServiceContext context) =>
        new(
            context.CommandPipeline.Value,
            context.GrainFactory,
            context.Storage,
            NullLogger<KernelWebhooksService>.Instance);

    static KernelSubscriptionsService CreateEventStoreSubscriptions(TestingServiceContext context) =>
        new(context.GrainFactory, context.Storage, Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()));
}
