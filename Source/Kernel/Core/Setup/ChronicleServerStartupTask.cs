// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for managing kernel event types.</param>
/// <param name="reactors"><see cref="IReactors"/> for managing kernel reactors.</param>
/// <param name="patternCapture"><see cref="IPatternCapture"/> for observing events for behavior pattern mining.</param>
/// <param name="projectionsServiceClient"><see cref="IProjectionsServiceClient"/> for registering projections with local silos.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="authenticationService"><see cref="IAuthenticationService"/> for managing authentication.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/> the retry backoff waits on; defaults to <see cref="TimeProvider.System"/>.</param>
/// <remarks>
/// Every step in <see cref="Execute"/> is a grain call that can land on a sibling silo, and every
/// silo runs this task as it starts. A sibling that has not stabilized yet answers none of them, and
/// an unhandled timeout here terminates the whole host - which is how one slow silo took both
/// replicas into a reinforcing crash loop: each restart took a new address the other silo then could
/// not reach either, while consuming applications saw nothing but timeouts. Retrying membership
/// instability as a class is what breaks that loop, and is why this does not special-case whichever
/// call site was unlucky enough to be the one that raced. See <see cref="SiblingSiloInstability"/>
/// for what counts as instability and what is still allowed to fail on the first attempt.
/// </remarks>
internal sealed class ChronicleServerStartupTask(
    IStorage storage,
    IEventTypes eventTypes,
    IReactors reactors,
    IPatternCapture patternCapture,
    IProjectionsServiceClient projectionsServiceClient,
    IGrainFactory grainFactory,
    IAuthenticationService authenticationService,
    ILogger<ChronicleServerStartupTask> logger,
    TimeProvider? timeProvider = null) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <summary>
    /// How many times a step is attempted before its failure is believed.
    /// </summary>
    /// <remarks>
    /// Each attempt already absorbs Orleans' own 30 second response timeout, and the backoff adds
    /// half a minute on top, so the budget spans several minutes of cluster formation - longer than
    /// membership normally takes to settle. A step still failing after that is not waiting on a
    /// sibling silo, so it is allowed to fail the host exactly as it did before.
    /// </remarks>
    const int MaxAttempts = 5;

    static readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(2);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            nameof(ChronicleServerStartupTask),
            ServiceLifecycleStage.Active,
            Execute);
    }

    async Task Execute(CancellationToken cancellationToken)
    {
        // Apply patches first before anything else starts
        var patchManager = grainFactory.GetGrain<IPatchManager>(0);
        await Step(nameof(IPatchManager.ApplyPatches), patchManager.ApplyPatches);

        await Step("EnsureDefaultSystemNamespace", grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault);

        // Register reactors for the system event store first, so ReactorsReactor can process EventStoreAdded/NamespaceAdded events
        await Step("DiscoverAndRegisterSystemReactors", () => reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default));

        var allEventStores = await storage.GetEventStores();
        foreach (var eventStore in allEventStores)
        {
            await Step("DiscoverAndRegisterEventTypes", () => eventTypes.DiscoverAndRegister(eventStore));
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            await Step("EnsureDefaultNamespace", namespaces.EnsureDefault);

            var eventStoreSubscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventStore);
            await Step("EnsureEventStoreSubscriptions", eventStoreSubscriptionsManager.Ensure);

            var readModelsManager = grainFactory.GetGrain<IReadModelsManager>(eventStore);
            await Step("EnsureReadModels", readModelsManager.Ensure);

            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            await Step("EnsureProjections", projectionsManager.Ensure);

            var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventStore);
            await Step("EnsureWebhooks", webhooksManager.Ensure);

            var capturesManager = grainFactory.GetGrain<ICapturesManager>(eventStore);
            await Step("EnsureCaptures", capturesManager.Ensure);

            var projectionDefinitions = await projectionsManager.GetProjectionDefinitions();
            await RegisterPersistedProjectionDefinitions(eventStore, projectionDefinitions);

            var rehydrateAll = (await namespaces.GetAll()).Select(async namespaceName =>
            {
                var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(namespaceName);
                if (!await namespaceStorage.HasData())
                {
                    // Nothing has ever been written to this namespace - there is no jobs, reactor subscriptions,
                    // event sequence state or observer to rehydrate. Skipping it avoids materializing its storage
                    // (for example creating a MongoDB database) for a namespace that has only ever been registered,
                    // never used. The moment it receives its first genuine write, that write lazily materializes
                    // whatever storage it needs on its own.
                    return;
                }

                await Step("DiscoverAndRegisterReactors", () => reactors.DiscoverAndRegister(eventStore, namespaceName));
                await Step("SubscribePatternCapture", () => patternCapture.Subscribe(eventStore, namespaceName));

                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                await Step("RehydrateJobs", jobsManager.Rehydrate);
                await Step("RehydrateEventSequences", grainFactory.GetEventSequences(eventStore, namespaceName).Rehydrate);
                await Step("RehydrateObservers", () => RehydrateReducerAndReactorObservers(eventStore, namespaceName));
            });
            await Task.WhenAll(rehydrateAll);
        }

        await authenticationService.EnsureDefaultAdminUser();
        await authenticationService.EnsureBootstrapClients();
#if DEVELOPMENT
        await authenticationService.EnsureDefaultClientCredentials();
#endif
    }

    /// <summary>
    /// Runs one startup step, riding out a sibling silo that has not stabilized yet.
    /// </summary>
    /// <param name="step">The name of the step, for the operator reading the log.</param>
    /// <param name="action">The work to perform.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only the failures <see cref="SiblingSiloInstability.IsTransient"/> recognizes are retried.
    /// Anything else - and anything still failing once the budget is spent - propagates, so a
    /// genuine defect still fails the host on startup rather than being started around.
    /// </remarks>
    async Task Step(string step, Func<Task> action)
    {
        var delay = _initialRetryDelay;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception exception) when (SiblingSiloInstability.IsTransient(exception) && attempt < MaxAttempts)
            {
                logger.RetryingStartupStep(exception, step, attempt, MaxAttempts, delay);
                await Task.Delay(delay, _timeProvider);
                delay += delay;
            }
            catch (Exception exception) when (SiblingSiloInstability.IsTransient(exception))
            {
                logger.StartupStepExhaustedRetries(exception, step, MaxAttempts);
                throw;
            }
        }
    }

    async Task RegisterPersistedProjectionDefinitions(EventStoreName eventStore, IEnumerable<ProjectionDefinition> projectionDefinitions)
    {
        var result = await projectionsServiceClient.Register(eventStore, projectionDefinitions);
        if (result.TryGetError(out var error))
        {
            foreach (var (identifier, failure) in error.Failures)
            {
                logger.FailedRegisteringPersistedProjectionDefinition(failure, identifier);
            }
        }
    }

    async Task RehydrateReducerAndReactorObservers(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var namespaceStorage = eventStoreStorage.GetNamespace(namespaceName);
        var knownObserverIds = (await namespaceStorage.Observers.GetAll()).Select(_ => _.Identifier).ToHashSet();
        var reducerDefinitions = await eventStoreStorage.Reducers.GetAll();
        var reactorDefinitions = await eventStoreStorage.Reactors.GetAll();

        var reducerObserverKeys = reducerDefinitions
            .Where(_ => knownObserverIds.Contains(_.Identifier))
            .Select(_ => new ObserverKey(_.Identifier, eventStore, namespaceName, _.EventSequenceId));
        var reactorObserverKeys = reactorDefinitions
            .Where(_ => knownObserverIds.Contains(_.Identifier))
            .Select(_ => new ObserverKey(_.Identifier, eventStore, namespaceName, _.EventSequenceId));
        var observerKeys = reducerObserverKeys.Concat(reactorObserverKeys).Distinct().ToArray();

        await Task.WhenAll(observerKeys.Select(_ => grainFactory.GetGrain<IObserver>(_).Ensure()));
    }
}
