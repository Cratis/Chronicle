// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an in-process implementation of <see cref="IServices"/> for testing scenarios.
/// </summary>
/// <remarks>
/// All gRPC service contracts are backed by no-op or minimal in-process implementations.
/// <see cref="IReadModels"/> is backed by an <see cref="InProcessReadModelsService"/> that returns
/// pre-seeded read model instances registered via the test scenario builder.
/// </remarks>
/// <param name="readModels">The <see cref="InProcessReadModelsService"/> used for read model lookups.</param>
internal sealed class TestingServices(InProcessReadModelsService readModels) : IServices
{
    static readonly IConstraints _constraints = new InProcessNoOpConstraintsService();
    static readonly IObservers _observers = new NoOpObserversService();
    static readonly IFailedPartitions _failedPartitions = new NoOpFailedPartitionsService();
    static readonly IReactors _reactors = new NoOpReactorsService();
    static readonly IReducers _reducers = new NoOpReducersService();
    static readonly IProjections _projections = new NoOpProjectionsService();
    static readonly IWebhooks _webhooks = new NoOpWebhooksService();
    static readonly IEventStoreSubscriptions _eventStoreSubscriptions = new NoOpEventStoreSubscriptionsService();
    static readonly IJobs _jobs = new NoOpJobsService();
    static readonly IEventSeeding _seeding = new NoOpEventSeedingService();
    static readonly IEventSequences _eventSequences = new NoOpEventSequencesService();
    static readonly IEventStores _eventStores = new NoOpEventStoresService();
    static readonly INamespaces _namespaces = new NoOpNamespacesService();
    static readonly IIdentities _identities = new NoOpIdentitiesService();
    static readonly IEventTypes _eventTypes = new NoOpEventTypesService();
    static readonly IRecommendations _recommendations = new NoOpRecommendationsService();
    static readonly IUsers _users = new NoOpUsersService();
    static readonly IApplications _applications = new NoOpApplicationsService();
    static readonly IServer _server = new NoOpServerService();

    /// <inheritdoc/>
    public IReadModels ReadModels => readModels;

    /// <inheritdoc/>
    public IConstraints Constraints => _constraints;

    /// <inheritdoc/>
    public IObservers Observers => _observers;

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => _failedPartitions;

    /// <inheritdoc/>
    public IReactors Reactors => _reactors;

    /// <inheritdoc/>
    public IReducers Reducers => _reducers;

    /// <inheritdoc/>
    public IProjections Projections => _projections;

    /// <inheritdoc/>
    public IWebhooks Webhooks => _webhooks;

    /// <inheritdoc/>
    public IEventStoreSubscriptions EventStoreSubscriptions => _eventStoreSubscriptions;

    /// <inheritdoc/>
    public IJobs Jobs => _jobs;

    /// <inheritdoc/>
    public IEventSeeding Seeding => _seeding;

    /// <inheritdoc/>
    public IEventSequences EventSequences => _eventSequences;

    /// <inheritdoc/>
    public IEventStores EventStores => _eventStores;

    /// <inheritdoc/>
    public INamespaces Namespaces => _namespaces;

    /// <inheritdoc/>
    public IIdentities Identities => _identities;

    /// <inheritdoc/>
    public IEventTypes EventTypes => _eventTypes;

    /// <inheritdoc/>
    public IRecommendations Recommendations => _recommendations;

    /// <inheritdoc/>
    public IUsers Users => _users;

    /// <inheritdoc/>
    public IApplications Applications => _applications;

    /// <inheritdoc/>
    public IServer Server => _server;

    sealed class NoOpObserversService : IObservers
    {
        public Task Replay(Replay command, CallContext context = default) => Task.CompletedTask;
        public Task ReplayPartition(ReplayPartition command, CallContext context = default) => Task.CompletedTask;
        public Task RetryPartition(RetryPartition command, CallContext context = default) => Task.CompletedTask;
        public Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default) =>
            Task.FromResult(new ObserverInformation());
        public Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<ObserverInformation>());
        public IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<ObserverInformation>>();
        public Task<IEnumerable<ObserverInformation>> GetReplayableObserversForEventTypes(GetReplayableObserversForEventTypesRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<ObserverInformation>());
    }

    sealed class NoOpFailedPartitionsService : IFailedPartitions
    {
        public Task<IEnumerable<FailedPartition>> GetFailedPartitions(GetFailedPartitionsRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<FailedPartition>());
        public IObservable<IEnumerable<FailedPartition>> ObserveFailedPartitions(GetFailedPartitionsRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<FailedPartition>>();
    }

    sealed class NoOpReactorsService : IReactors
    {
        public IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default) =>
            Observable.Never<EventsToObserve>();
        public Task<HasReactorResponse> HasReactor(HasReactorRequest request, CallContext context = default) =>
            Task.FromResult(new HasReactorResponse { Exists = false });
    }

    sealed class NoOpReducersService : IReducers
    {
        public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default) =>
            Observable.Never<ReduceOperationMessage>();
    }

    sealed class NoOpProjectionsService : IProjections
    {
        public Task Register(RegisterRequest request, CallContext context = default) => Task.CompletedTask;
        public Task<IEnumerable<ProjectionDefinition>> GetAllDefinitions(GetAllDefinitionsRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<ProjectionDefinition>());
        public Task<IEnumerable<ProjectionWithDeclaration>> GetAllDeclarations(GetAllDeclarationsRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<ProjectionWithDeclaration>());
        public Task<OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors>> Preview(PreviewProjectionRequest request, CallContext context = default) =>
            Task.FromResult(new OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors>(new ProjectionPreview()));
        public Task<SaveProjectionResult> Save(SaveProjectionRequest request, CallContext context = default) =>
            Task.FromResult(new SaveProjectionResult());
        public Task<OneOf<GeneratedCode, ProjectionDeclarationParsingErrors>> GenerateDeclarativeCode(GenerateDeclarativeCodeRequest request, CallContext context = default) =>
            Task.FromResult(new OneOf<GeneratedCode, ProjectionDeclarationParsingErrors>(new GeneratedCode()));
        public Task<OneOf<GeneratedCode, ProjectionDeclarationParsingErrors>> GenerateModelBoundCode(GenerateModelBoundCodeRequest request, CallContext context = default) =>
            Task.FromResult(new OneOf<GeneratedCode, ProjectionDeclarationParsingErrors>(new GeneratedCode()));
    }

    sealed class NoOpWebhooksService : IWebhooks
    {
        public Task Add(AddWebhooks request, CallContext context = default) => Task.CompletedTask;
        public Task Remove(RemoveWebhooks request, CallContext context = default) => Task.CompletedTask;
        public Task<IEnumerable<WebhookDefinition>> GetWebhooks(GetWebhooksRequest request) =>
            Task.FromResult(Enumerable.Empty<WebhookDefinition>());
        public IObservable<IEnumerable<WebhookDefinition>> ObserveWebhooks(GetWebhooksRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<WebhookDefinition>>();
        public Task<TestOAuthAuthorizationResponse> TestOAuthAuthorization(TestOAuthAuthorizationRequest request, CallContext context = default) =>
            Task.FromResult(new TestOAuthAuthorizationResponse());
        public Task<TestWebhookResponse> TestWebhook(TestWebhookRequest request, CallContext context = default) =>
            Task.FromResult(new TestWebhookResponse());
    }

    sealed class NoOpEventStoreSubscriptionsService : IEventStoreSubscriptions
    {
        public Task Add(AddEventStoreSubscriptions request, CallContext context = default) => Task.CompletedTask;
        public Task Remove(RemoveEventStoreSubscriptions request, CallContext context = default) => Task.CompletedTask;
        public Task<IEnumerable<EventStoreSubscriptionDefinition>> GetSubscriptions(GetEventStoreSubscriptionsRequest request) =>
            Task.FromResult(Enumerable.Empty<EventStoreSubscriptionDefinition>());
    }

    sealed class NoOpJobsService : IJobs
    {
        public Task Stop(StopJob command, CallContext context = default) => Task.CompletedTask;
        public Task Resume(ResumeJob command, CallContext context = default) => Task.CompletedTask;
        public Task Delete(DeleteJob command, CallContext context = default) => Task.CompletedTask;
        public Task<OneOf<Job, JobError>> GetJob(GetJobRequest request, CallContext context = default) =>
            Task.FromResult(new OneOf<Job, JobError>(new Job()));
        public Task<IEnumerable<Job>> GetJobs(GetJobsRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<Job>());
        public IObservable<IEnumerable<Job>> ObserveJobs(GetJobsRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<Job>>();
        public Task<IEnumerable<JobStep>> GetJobSteps(GetJobStepsRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<JobStep>());
    }

    sealed class NoOpEventSeedingService : IEventSeeding
    {
        public Task Seed(SeedRequest request, CallContext context = default) => Task.CompletedTask;
        public Task<SeedDataResponse> GetGlobalSeedData(GetSeedDataRequest request, CallContext context = default) =>
            Task.FromResult(new SeedDataResponse());
        public Task<SeedDataResponse> GetNamespaceSeedData(GetSeedDataRequest request, CallContext context = default) =>
            Task.FromResult(new SeedDataResponse());
    }

    sealed class NoOpEventSequencesService : IEventSequences
    {
        public Task<AppendResponse> Append(AppendRequest request, CallContext context = default) =>
            Task.FromResult(new AppendResponse());
        public Task<AppendManyResponse> AppendMany(AppendManyRequest request, CallContext context = default) =>
            Task.FromResult(new AppendManyResponse());
        public Task<GetTailSequenceNumberResponse> GetTailSequenceNumber(GetTailSequenceNumberRequest request, CallContext context = default) =>
            Task.FromResult(new GetTailSequenceNumberResponse());
        public Task<GetForEventSourceIdAndEventTypesResponse> GetForEventSourceIdAndEventTypes(GetForEventSourceIdAndEventTypesRequest request, CallContext context = default) =>
            Task.FromResult(new GetForEventSourceIdAndEventTypesResponse());
        public Task<HasEventsForEventSourceIdResponse> HasEventsForEventSourceId(HasEventsForEventSourceIdRequest request, CallContext context = default) =>
            Task.FromResult(new HasEventsForEventSourceIdResponse());
        public Task<GetFromEventSequenceNumberResponse> GetEventsFromEventSequenceNumber(GetFromEventSequenceNumberRequest request, CallContext context = default) =>
            Task.FromResult(new GetFromEventSequenceNumberResponse());
        public Task Revise(ReviseRequest request, CallContext context = default) => Task.CompletedTask;
        public Task<RedactResponse> Redact(RedactRequest request, CallContext context = default) =>
            Task.FromResult(new RedactResponse());
        public Task RedactForEventSource(RedactForEventSourceRequest request, CallContext context = default) => Task.CompletedTask;
    }

    sealed class NoOpEventStoresService : IEventStores
    {
        public Task<IEnumerable<string>> GetEventStores() => Task.FromResult(Enumerable.Empty<string>());
        public IObservable<IEnumerable<string>> ObserveEventStores(CallContext callContext = default) =>
            Observable.Empty<IEnumerable<string>>();
        public Task Ensure(EnsureEventStore command) => Task.CompletedTask;
    }

    sealed class NoOpNamespacesService : INamespaces
    {
        public Task Ensure(EnsureNamespace command) => Task.CompletedTask;
        public Task<IEnumerable<string>> GetNamespaces(GetNamespacesRequest request) =>
            Task.FromResult(Enumerable.Empty<string>());
        public IObservable<IEnumerable<string>> ObserveNamespaces(GetNamespacesRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<string>>();
    }

    sealed class NoOpIdentitiesService : IIdentities
    {
        public Task<IEnumerable<Identity>> GetIdentities(GetIdentitiesRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<Identity>());
        public IObservable<IEnumerable<Identity>> ObserveIdentities(GetIdentitiesRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<Identity>>();
    }

    sealed class NoOpEventTypesService : IEventTypes
    {
        public Task Register(RegisterEventTypesRequest request) => Task.CompletedTask;
        public Task RegisterSingle(RegisterSingleEventTypeRequest request) => Task.CompletedTask;
        public Task<IEnumerable<EventType>> GetAll(GetAllEventTypesRequest request) =>
            Task.FromResult(Enumerable.Empty<EventType>());
        public Task<IEnumerable<EventTypeRegistration>> GetAllRegistrations(GetAllEventTypesRequest request) =>
            Task.FromResult(Enumerable.Empty<EventTypeRegistration>());
        public IObservable<IEnumerable<EventTypeRegistration>> ObserveAllRegistrations(GetAllEventTypesRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<EventTypeRegistration>>();
        public Task<IEnumerable<EventTypeRegistration>> GetAllGenerationsForEventType(GetEventTypeGenerationsRequest request) =>
            Task.FromResult(Enumerable.Empty<EventTypeRegistration>());
    }

    sealed class NoOpRecommendationsService : IRecommendations
    {
        public Task Perform(Perform command, CallContext context = default) => Task.CompletedTask;
        public Task Ignore(Perform command, CallContext context = default) => Task.CompletedTask;
        public Task<IEnumerable<Recommendation>> GetRecommendations(GetRecommendationsRequest request, CallContext context = default) =>
            Task.FromResult(Enumerable.Empty<Recommendation>());
        public IObservable<IEnumerable<Recommendation>> ObserveRecommendations(GetRecommendationsRequest request, CallContext context = default) =>
            Observable.Empty<IEnumerable<Recommendation>>();
    }

    sealed class NoOpUsersService : IUsers
    {
        public Task Add(AddUser command) => Task.CompletedTask;
        public Task Remove(RemoveUser command) => Task.CompletedTask;
        public Task ChangePassword(ChangeUserPassword command) => Task.CompletedTask;
        public Task RequirePasswordChange(RequirePasswordChange command) => Task.CompletedTask;
        public Task SetInitialAdminPassword(SetInitialAdminPassword command) => Task.CompletedTask;
        public Task<IList<User>> GetAll() => Task.FromResult<IList<User>>([]);
        public IObservable<IList<User>> ObserveAll(CallContext context = default) =>
            Observable.Empty<IList<User>>();
        public Task<InitialAdminPasswordSetupStatus> GetInitialAdminPasswordSetupStatus() =>
            Task.FromResult(new InitialAdminPasswordSetupStatus());
    }

    sealed class NoOpApplicationsService : IApplications
    {
        public Task Add(AddApplication command) => Task.CompletedTask;
        public Task Remove(RemoveApplication command) => Task.CompletedTask;
        public Task ChangeSecret(ChangeApplicationSecret command) => Task.CompletedTask;
        public Task<IList<Application>> GetAll() => Task.FromResult<IList<Application>>([]);
        public IObservable<IList<Application>> ObserveAll(CallContext context = default) =>
            Observable.Empty<IList<Application>>();
    }

    sealed class NoOpServerService : IServer
    {
        public Task ReloadState() => Task.CompletedTask;
        public Task<ServerVersionInfo> GetVersionInfo() => Task.FromResult(new ServerVersionInfo());
    }
}
