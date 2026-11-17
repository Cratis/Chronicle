// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Patterns;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KernelApplicationsService = KernelGrpc::Cratis.Chronicle.Services.Security.Applications;
using KernelCapturesService = KernelGrpc::Cratis.Chronicle.Services.Captures.Captures;
using KernelComplianceAuthorization = KernelGrpc::Cratis.Chronicle.Services.Compliance.ComplianceAuthorization;
using KernelComplianceService = KernelGrpc::Cratis.Chronicle.Services.Compliance.ComplianceService;
using KernelConstraintsService = KernelGrpc::Cratis.Chronicle.Services.Events.Constraints.Constraints;
using KernelEventSourcesService = KernelGrpc::Cratis.Chronicle.Services.EventSources.EventSources;
using KernelEventStoresService = KernelGrpc::Cratis.Chronicle.Services.EventStores.EventStores;
using KernelEventTypesService = KernelGrpc::Cratis.Chronicle.Services.EventTypes.EventTypes;
using KernelExternalServicesService = KernelGrpc::Cratis.Chronicle.Services.ExternalServices.ExternalServices;
using KernelFacetSetGenerator = KernelCore::Cratis.Chronicle.Patterns.FacetSetGenerator;
using KernelFacetVocabulary = KernelCore::Cratis.Chronicle.Patterns.FacetVocabulary;
using KernelIdentitiesService = KernelGrpc::Cratis.Chronicle.Services.Identities.Identities;
using KernelJobsService = KernelGrpc::Cratis.Chronicle.Services.Jobs.Jobs;
using KernelMaterializedReadModelStore = KernelCore::Cratis.Chronicle.ReadModels.MaterializedReadModelStore;
using KernelNamespacesService = KernelGrpc::Cratis.Chronicle.Services.Namespaces.Namespaces;
using KernelOccurrencesService = KernelGrpc::Cratis.Chronicle.Services.Occurrences.Occurrences;
using KernelPatternMatcher = KernelCore::Cratis.Chronicle.Patterns.PatternMatcher;
using KernelPatternsService = KernelGrpc::Cratis.Chronicle.Services.Patterns.Patterns;
using KernelProjectionChangesetMediator = KernelCore::Cratis.Chronicle.Projections.ProjectionChangesetMediator;
using KernelReadModelsService = KernelGrpc::Cratis.Chronicle.Services.ReadModels.ReadModels;
using KernelRecommendationsService = KernelGrpc::Cratis.Chronicle.Services.Recommendations.Recommendations;
using KernelReducerMediator = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.ReducerMediator;
using KernelSeedingService = KernelGrpc::Cratis.Chronicle.Services.Seeding.EventSeeding;
using KernelSequencesService = KernelGrpc::Cratis.Chronicle.Services.Sequences.EventSequences;
using KernelServerService = KernelGrpc::Cratis.Chronicle.Services.Host.Server;
using KernelUsersService = KernelGrpc::Cratis.Chronicle.Services.Security.Users;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Creates the non-observation kernel gRPC services backed by in-memory storage.
/// </summary>
// The factories deliberately return the contract interface the lazily resolved services are exposed as.
#pragma warning disable CA1859
internal sealed partial class TestingServices
{
    /// <summary>The in-process test kernel enforces no store entitlements, so query collaborators resolve nothing extra.</summary>
    static readonly IServiceProvider _noQueryServices = new ServiceCollection().BuildServiceProvider();

    static IExternalServices CreateExternalServices(TestingServiceContext context) =>
        new KernelExternalServicesService(context.CommandPipeline.Value, context.Storage, NullLogger<KernelExternalServicesService>.Instance);

    static ICaptures CreateCaptures(TestingServiceContext context) =>
        new KernelCapturesService(
            context.CommandPipeline.Value,
            context.Storage,
            context.JsonSerializerOptions,
            context.NamespaceCasing,
            NullLogger<KernelCapturesService>.Instance);

    static Contracts.Occurrences.IOccurrences CreateOccurrences(TestingServiceContext context) =>
        new Occurrences.OccurrenceReplies(new KernelOccurrencesService(context.CommandPipeline.Value, context.Storage, context.NamespaceCasing, NullLogger<KernelOccurrencesService>.Instance), context.Replies ?? new());

    static IJobs CreateJobs(TestingServiceContext context) =>
        new KernelJobsService(context.CommandPipeline.Value, context.Storage, context.GrainFactory, context.NamespaceCasing, NullLogger<KernelJobsService>.Instance);

    static IEventSeeding CreateSeeding(TestingServiceContext context) =>
        new KernelSeedingService(context.CommandPipeline.Value, context.GrainFactory, context.NamespaceCasing, NullLogger<KernelSeedingService>.Instance);

    static Contracts.Sequences.IEventSequences CreateSequences(TestingServiceContext context) =>
        new EventSequences.SequenceReplies(
            new KernelSequencesService(
                context.CommandPipeline.Value,
                context.Storage,
                context.Compliance.CreateEventCompliance(context.Storage),
                context.JsonSerializerOptions,
                new EventSequences.InProcessQueryContextManager(),
                context.GrainFactory,
                context.NamespaceCasing,
                NullLogger<KernelSequencesService>.Instance),
            context.Replies ?? new());

    static INamespaces CreateNamespaces(TestingServiceContext context) =>
        new KernelNamespacesService(context.CommandPipeline.Value, context.Storage, NullLogger<KernelNamespacesService>.Instance);

    static IIdentities CreateIdentities(TestingServiceContext context) =>
        new KernelIdentitiesService(context.CommandPipeline.Value, context.Storage, context.NamespaceCasing, NullLogger<KernelIdentitiesService>.Instance);

    static IEventSources CreateEventSources(TestingServiceContext context) =>
        new KernelEventSourcesService(
            context.CommandPipeline.Value,
            context.Storage,
            NullLogger<KernelEventSourcesService>.Instance);

    static IEventTypes CreateEventTypes(TestingServiceContext context) =>
        new KernelEventTypesService(
            context.CommandPipeline.Value,
            context.Storage,
            NullLogger<KernelEventTypesService>.Instance);

    static IPatterns CreatePatterns(TestingServiceContext context)
    {
        var patternOptions = Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions());
        return new KernelPatternsService(
            context.Storage,
            new KernelFacetVocabulary(patternOptions),
            new KernelFacetSetGenerator(),
            new KernelPatternMatcher(),
            patternOptions,
            context.NamespaceCasing,
            NullLogger<KernelPatternsService>.Instance);
    }

    static IRecommendations CreateRecommendations(TestingServiceContext context) =>
        new KernelRecommendationsService(context.CommandPipeline.Value, context.Storage, context.NamespaceCasing, NullLogger<KernelRecommendationsService>.Instance);

    static IConstraints CreateConstraints(TestingServiceContext context) =>
        new KernelConstraintsService(context.GrainFactory);

    static IUsers CreateUsers(TestingServiceContext context) =>
        new KernelUsersService(
            context.CommandPipeline.Value,
            context.Storage,
            Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()),
            NullLogger<KernelUsersService>.Instance);

    static IApplications CreateApplications(TestingServiceContext context) =>
        new KernelApplicationsService(context.CommandPipeline.Value, context.Storage, NullLogger<KernelApplicationsService>.Instance);

    static IServer CreateServer() => new KernelServerService(null!);

    static IEventStores CreateEventStores(TestingServiceContext context) =>
        new KernelEventStoresService(context.CommandPipeline.Value, context.GrainFactory, context.Storage, new KernelCore::Cratis.Chronicle.EventStores.EventStoreInitialization(context.Storage, context.GrainFactory, NullLogger<KernelCore::Cratis.Chronicle.EventStores.EventStoreInitialization>.Instance), _noQueryServices, NullLogger<KernelEventStoresService>.Instance);

    static IReadModels CreateReadModels(TestingServiceContext context) =>
        new KernelReadModelsService(
            context.GrainFactory,
            context.Storage,
            context.ReadBindings,
            new ExpandoObjectConverter(new TypeFormats()),
            new KernelReducerMediator(),
            new KernelProjectionChangesetMediator(),

            // Live read-model watching is not supported by the in-process scenario harness (grain and
            // object-reference lookups throw NotSupportedException), so no local silo details are needed.
            null!,
            context.Compliance.CreateReadModelsCompliance(),
            new KernelMaterializedReadModelStore(context.ReadBindings, context.Compliance.CreateReadModelsCompliance()),
            context.JsonSerializerOptions,
            context.NamespaceCasing);

    static ICompliance CreateCompliance(TestingServiceContext context) =>
        new KernelComplianceService(
            context.GrainFactory,
            context.Compliance.Manager,
            NullLogger<KernelComplianceService>.Instance,
            new KernelComplianceAuthorization(
                context.KernelOptions,
                context.Storage,
                null!, // Direct in-process calls bypass authorization and never resolve a request scope.
                NullLogger<KernelComplianceAuthorization>.Instance));
}
#pragma warning restore CA1859
