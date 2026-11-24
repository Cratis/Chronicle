// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ClientEventLog = Cratis.Chronicle.EventSequences.IEventLog;
using KernelCaptureLanguageService = KernelCore::Cratis.Chronicle.Captures.Engine.DeclarationLanguage.LanguageService;
using KernelCaptureValidator = KernelCore::Cratis.Chronicle.Captures.Engine.CaptureValidator;
using KernelEventTypeRegistrar = KernelCore::Cratis.Chronicle.EventTypes.EventTypeRegistrar;
using KernelWebhookComparer = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookDefinitionComparer;
using KernelWebhookMediatorImpl = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookMediator;
using KernelWebhookRegistrar = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookRegistrar;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Creates the one Arc command pipeline that serves every command-dispatching test service.
/// </summary>
/// <remarks>
/// Its provider carries the collaborators the command handlers resolve their parameters from - the same
/// instances the service constructors used to receive directly before dispatch moved to the Arc command pipeline.
/// </remarks>
internal static class TestingCommandPipeline
{
    /// <summary>
    /// Creates the command pipeline for the given <see cref="TestingServiceContext"/>.
    /// </summary>
    /// <param name="context">The shared collaborators the pipeline is wired from.</param>
    /// <returns>The <see cref="Arc.Commands.ICommandPipeline"/>.</returns>
    internal static Arc.Commands.ICommandPipeline Create(TestingServiceContext context) =>
        InProcessCommandPipeline.Create(
            context.GrainFactory,
            context.Storage,
            context.JsonSerializerOptions,
            services => RegisterCollaborators(context, services));

    static void RegisterCollaborators(TestingServiceContext context, IServiceCollection services)
    {
        var grainFactory = context.GrainFactory;
        var storage = context.Storage;
        var compliance = context.Compliance;

        services.AddSingleton(context.NamespaceIncarnations);
        services.AddSingleton<KernelCore::Cratis.Chronicle.Events.IEventCompliance>(compliance.CreateEventCompliance(storage));
        services.AddSingleton<KernelCore::Cratis.Chronicle.Schemas.IJsonSchemaMetadataManager>(compliance.Manager);
        services.AddSingleton<KernelCore::Cratis.Chronicle.EventTypes.IEventTypesCacheClient>(new EventSequences.NoOpEventTypesCacheClient());
        services.AddSingleton<KernelCore::Cratis.Chronicle.Observation.IObserverDefinitionsCacheClient>(new NoOpObserverDefinitionsCacheClient());
        services.AddSingleton(new KernelEventTypeRegistrar(grainFactory));
        services.AddSingleton<KernelCore::Cratis.Chronicle.Captures.Engine.DeclarationLanguage.ILanguageService>(new KernelCaptureLanguageService());
        services.AddSingleton<KernelCore::Cratis.Chronicle.Captures.Engine.ICaptureValidator>(new KernelCaptureValidator(storage));
        AddInitialAdminSetupServices(services).AddSingleton(CreateWebhookRegistrar(grainFactory, storage, context.JsonSerializerOptions));

        // AddCratisArcCore discovers every ICommandExecutionScope across the whole process, not just the
        // ones this pipeline cares about - an Arc.Chronicle consumer's transactional command scope is
        // discovered here too, even though this pipeline only ever executes the kernel's own commands,
        // which append directly through the grain and never touch a unit of work. Without a
        // registration, that scope falls back to auto-activating the real client EventStore, which
        // needs a live connection this in-process kernel does not have. This satisfies the resolution
        // harmlessly instead.
        services.AddSingleton<IUnitOfWorkManager>(new EventSequences.NoOpUnitOfWorkManager());
        services.AddSingleton<ClientEventLog>(new EventSequences.NoOpEventLog());
        services.AddSingleton(context.EventTypes());
    }

    static IServiceCollection AddInitialAdminSetupServices(IServiceCollection services) =>
        services.AddDataProtection().Services.AddSingleton<KernelCore::Cratis.Chronicle.Security.IInitialAdminSetupTokens, KernelCore::Cratis.Chronicle.Security.InitialAdminSetupTokens>();

    static KernelWebhookRegistrar CreateWebhookRegistrar(IGrainFactory grainFactory, IStorage storage, JsonSerializerOptions jsonSerializerOptions) =>
        new(
            grainFactory,
            new KernelWebhookComparer(
                storage,
                new ObjectComparer(),
                NullLogger<KernelWebhookComparer>.Instance),
            null!,
            null!,
            new KernelWebhookMediatorImpl(null!, jsonSerializerOptions),
            Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()),
            storage,
            NullLogger<KernelWebhookRegistrar>.Instance);
}
