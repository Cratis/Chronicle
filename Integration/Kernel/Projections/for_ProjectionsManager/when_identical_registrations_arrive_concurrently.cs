// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Cratis.Monads;

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_identical_registrations_arrive_concurrently.context;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using KernelEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using KernelNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using KernelProjectionId = Cratis.Chronicle.Concepts.Projections.ProjectionId;
using ProjectionRegistrationError = Cratis.Chronicle.Projections.Engine.ProjectionRegistrationError;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager;

[Collection(given.SharedRegistrationCollection.Name)]
public class when_identical_registrations_arrive_concurrently(context context) : Given<context>(context)
{
    public class context(given.SharedRegistrationFixture fixture) : Specification<given.SharedRegistrationFixture>(fixture)
    {
        const int Callers = 16;
        readonly RegistrationObserver _observer = new(Callers - 1);
        GatedProjectionsServiceClient _engine;
        IProjectionsManager _manager;
        ProjectionDefinition _definition;
        Task[] _registrations;

        public override bool AutoDiscoverArtifacts => false;

        protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        {
            builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
            builder.SiloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<ILogger<ProjectionsManager>>(_observer);
                services.AddSingleton<IProjectionsServiceClient>(provider =>
                {
                    _engine = new(ActivatorUtilities.CreateInstance<ProjectionsServiceClient>(provider));
                    return _engine;
                });
            });
        };

        async Task Establish()
        {
            var store = await ChronicleClient.GetEventStore($"shared-{Guid.NewGuid():N}", "tenant");
            await store.DiscoverAll();
            await store.RegisterAll();
            (await store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
            var grains = Services.GetRequiredService<IGrainFactory>();
            _manager = grains.GetGrain<IProjectionsManager>(store.Name.Value);
            var original = (await _manager.GetProjectionDefinitions()).First(definition => definition.Identifier.Value == "$system.statistics.event-types");
            _definition = original with { IsRewindable = !original.IsRewindable };
            _engine.Enabled = true;
        }

        async Task Because()
        {
            var definitions = new[] { _definition };
            var signature = ProjectionRegistrationSignature.For(definitions, null, "the-app", "1.0");
            _registrations = Enumerable.Range(0, Callers).Select(_ => _manager.RegisterShared(signature, definitions, null, "the-app", "1.0")).ToArray();
            try
            {
                await _engine.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                await _observer.AllJoined.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            finally
            {
                _engine.Release.TrySetResult();
            }
            await Task.WhenAll(_registrations).WaitAsync(TimeSpan.FromSeconds(30));
        }

        public void ShouldCompleteEveryCaller() => _registrations.All(registration => registration.IsCompletedSuccessfully).ShouldBeTrue();
        public void ShouldShareOneEngineRegistration() => _engine.Registrations.ShouldEqual(1);
        public void ShouldJoinTheRealGrainSelfCall() => _observer.Joined.ShouldEqual(Callers - 1);
        public async Task ShouldStoreTheProjection()
        {
            var stored = (await _manager.GetProjectionDefinitions()).Single(definition => definition.Identifier == _definition.Identifier);
            stored.IsRewindable.ShouldEqual(_definition.IsRewindable);
            stored.Version.ShouldEqual("1.0");
        }
    }

    [Fact] void should_complete_every_caller() => Context.ShouldCompleteEveryCaller();
    [Fact] void should_register_with_the_engine_once() => Context.ShouldShareOneEngineRegistration();
    [Fact] void should_join_the_real_grain_self_call() => Context.ShouldJoinTheRealGrainSelfCall();
    [Fact] Task should_store_the_projection() => Context.ShouldStoreTheProjection();

    sealed class RegistrationObserver(int expectedJoins) : ILogger<ProjectionsManager>
    {
        public readonly TaskCompletionSource AllJoined = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Joined;

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == "JoiningRegistrationInFlight" && Interlocked.Increment(ref Joined) == expectedJoins)
            {
                AllJoined.TrySetResult();
            }
        }
    }

    sealed class GatedProjectionsServiceClient(IProjectionsServiceClient inner) : IProjectionsServiceClient
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Enabled;
        public int Registrations;

        public async Task<Result<ProjectionRegistrationError>> Register(KernelEventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions)
        {
            if (Enabled)
            {
                Interlocked.Increment(ref Registrations);
                Entered.TrySetResult();
                await Release.Task;
            }
            return await inner.Register(eventStore, definitions);
        }

        public Task Unregister(KernelEventStoreName eventStore, KernelProjectionId projectionId) => inner.Unregister(eventStore, projectionId);
        public Task NamespaceAdded(KernelEventStoreName eventStore, KernelNamespaceName @namespace) => inner.NamespaceAdded(eventStore, @namespace);
    }
}
