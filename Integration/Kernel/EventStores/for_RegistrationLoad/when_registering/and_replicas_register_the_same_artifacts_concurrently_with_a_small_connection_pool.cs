// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Options;

using ChronicleConnection = Cratis.Chronicle.XUnit.Integration.ChronicleConnection;
using context = Cratis.Chronicle.Kernel.Integration.EventStores.for_RegistrationLoad.when_registering.and_replicas_register_the_same_artifacts_concurrently_with_a_small_connection_pool.context;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_RegistrationLoad.when_registering;

/// <summary>
/// Registers one reactor, one reducer and one projection from several client replicas in several namespaces at once,
/// against a kernel whose MongoDB pool allows 5 connections and a wait queue of 5. Every registration must succeed within
/// the bound and a probe registration plus append afterwards must succeed. The wait-queue-full log count is diagnostic only.
/// The bound is deliberately tolerant; it catches a stall, not a small regression.
/// </summary>
/// <param name="context">The context.</param>
[Collection(given.RegistrationLoadCollection.Name)]
public class and_replicas_register_the_same_artifacts_concurrently_with_a_small_connection_pool(context context) : Given<context>(context)
{
    public class context(given.RegistrationLoadFixture fixture) : Specification<given.RegistrationLoadFixture>(fixture)
    {
        public const int Replicas = 3;
        public const int Namespaces = 2;
        public static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

        readonly List<IChronicleClient> _clients = [];

        readonly WaitQueueFullLogDetector _logs = new();
        public bool WorkloadTimedOut;
        public bool ProbeTimedOut;
        public bool ProbeRegistered;
        public bool ProbeAppended;
        public Exception? ProbeFailure;
        public int Registrations;
        public int Succeeded;
        public int Completed;
        public Exception[] Failures = [];
        public TimeSpan Elapsed;
        public int LoggedEntries => _logs.Entries;

        /// <summary>
        /// Diagnostic only: the number of kernel log entries carrying a MongoWaitQueueFullException. Observer activation and
        /// event sequence reads triggered by registration are not covered by the registration gate. This becomes a strict
        /// assertion (zero) once https://github.com/Cratis/Chronicle/issues/5650 is fixed.
        /// </summary>
        public int WaitQueueFullEntries => _logs.WaitQueueFullEntries;

        public override bool AutoDiscoverArtifacts => false;
        public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];
        public override IEnumerable<Type> Reactors => [typeof(given.LoadArtifacts.LoadReactor)];
        public override IEnumerable<Type> Reducers => [typeof(given.LoadArtifacts.LoadReducer)];
        public override IEnumerable<Type> Projections => [typeof(given.LoadArtifacts.LoadProjection)];

        protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        {
            // The pool is restricted for this kernel only; the shared fixtures keep the driver defaults.
            var server = $"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true&maxPoolSize=5&waitQueueMultiple=1";
            builder.WithMongoDB(server, Constants.EventStore, databaseNamePrefix: DatabaseNamePrefix);
            builder.SiloBuilder.ConfigureServices(services =>
            {
                services.PostConfigure<MongoDBOptions>(options => options.Server = server);
                services.AddSingleton<ILoggerProvider>(_logs);
            });
        };

        public override void Dispose()
        {
            base.Dispose();
            _logs.Dispose();
        }

        protected override Task OnDisposeAsync()
        {
            // Every client this spec built is disposed before the base teardown disposes the per-context kernel.
            foreach (var client in _clients.OfType<IDisposable>())
            {
                client.Dispose();
            }

            _clients.Clear();
            return base.OnDisposeAsync();
        }

        async Task Because()
        {
            var eventStoreName = $"rl-{Guid.NewGuid().ToString("N")[..8]}";
            var replicas = Enumerable.Range(0, Replicas).Select(_ => CreateReplica()).ToArray();
            var registrations = replicas.SelectMany(replica => Enumerable.Range(0, Namespaces).Select(index => (replica, @namespace: $"tenant-{index}"))).ToArray();
            Registrations = registrations.Length;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // One deadline covers the whole concurrent workload: getting the stores, registering and waiting.
                var results = await Task.WhenAll(registrations.Select(_ => Register(_.replica, eventStoreName, _.@namespace))).WaitAsync(Bound);
                Elapsed = stopwatch.Elapsed;
                Failures = [.. results.OfType<Exception>()];
                Succeeded = results.Count(_ => _ is null);
                Completed = results.Length;
            }
            catch (TimeoutException ex)
            {
                WorkloadTimedOut = true;
                Elapsed = stopwatch.Elapsed;
                Failures = [ex];
            }

            await Probe(eventStoreName);
            Console.WriteLine($"DIAGNOSTIC wait-queue-full log entries: {WaitQueueFullEntries}");
        }

        async Task Probe(string eventStoreName)
        {
            // A fresh connection registers the same set in a new namespace and appends an event, showing the kernel recovered.
            try
            {
                // A separate deadline covers the whole probe: registration and the append.
                await RunProbe(eventStoreName).WaitAsync(Bound);
            }
            catch (TimeoutException ex)
            {
                ProbeTimedOut = true;
                ProbeFailure = ex;
            }
            catch (Exception ex)
            {
                ProbeFailure = ex;
            }
        }

        async Task RunProbe(string eventStoreName)
        {
            var store = await CreateReplica().GetEventStore(eventStoreName, "probe");
            await store.DiscoverAll();
            await store.RegisterAll();
            ProbeRegistered = (await store.WaitForRegistration(Bound)).IsSuccess;
            var append = await store.EventLog.Append("customer", new CustomerNamed("Probe"));
            ProbeAppended = append.IsSuccess;
        }

        IChronicleClient CreateReplica()
        {
            var loggerFactory = Services.GetRequiredService<ILoggerFactory>();
            var connection = new ChronicleConnection(
                new ConnectionLifecycle(loggerFactory.CreateLogger<ConnectionLifecycle>()),
                Services.GetRequiredService<IGrainFactory>(),
                Services.GetRequiredService<ILocalSiloDetails>(),
                loggerFactory);
            connection.SetServices(((IChronicleServicesAccessor)Services.GetRequiredService<IChronicleConnection>()).Services);
            var client = new ChronicleClient(
                connection,
                Services.GetRequiredService<IOptions<ChronicleOptions>>().Value,
                Services.GetRequiredService<IClientArtifactsProvider>(),
                Services,
                Services.GetRequiredService<IIdentityProvider>(),
                loggerFactory: loggerFactory);
            _clients.Add(client);
            return client;
        }

        async Task<Exception?> Register(IChronicleClient client, string eventStoreName, string @namespace)
        {
            try
            {
                await Task.Yield();
                var store = await client.GetEventStore(eventStoreName, @namespace);
                if (!store.Registration.HasRun)
                {
                    await store.DiscoverAll();
                    await store.RegisterAll();
                }

                var outcome = await store.WaitForRegistration(Bound);
                return outcome.IsSuccess ? null : new InvalidOperationException($"Registration did not succeed for {eventStoreName}/{@namespace}: {outcome}");
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }

    [Fact] void should_not_fail_any_registration() => Context.Failures.ShouldBeEmpty();
    [Fact] void should_complete_every_registration() => Context.Succeeded.ShouldEqual(context.Replicas * context.Namespaces);
    [Fact] void should_not_time_out_the_workload() => Context.WorkloadTimedOut.ShouldBeFalse();
    [Fact] void should_not_time_out_the_probe() => Context.ProbeTimedOut.ShouldBeFalse();
    [Fact] void should_complete_within_the_bound() => Context.Elapsed.ShouldBeLessThan(context.Bound);
    [Fact] void should_register_a_probe_from_a_new_connection() => Context.ProbeRegistered.ShouldBeTrue();
    [Fact] void should_append_an_event_after_the_load() => Context.ProbeAppended.ShouldBeTrue();
    [Fact] void should_not_fail_the_probe() => Context.ProbeFailure.ShouldBeNull();

    /// <summary>
    /// Counts kernel log entries, and those carrying a MongoWaitQueueFullException in their message or exception chain.
    /// </summary>
    sealed class WaitQueueFullLogDetector : ILoggerProvider, ILogger
    {
        const string ExceptionName = "MongoWaitQueueFullException";
        int _entries;
        int _waitQueueFullEntries;

        public int Entries => _entries;
        public int WaitQueueFullEntries => _waitQueueFullEntries;

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Interlocked.Increment(ref _entries);
            if ((exception?.ToString().Contains(ExceptionName, StringComparison.Ordinal) ?? false) ||
                formatter(state, exception).Contains(ExceptionName, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _waitQueueFullEntries);
            }
        }

        public void Dispose()
        {
        }
    }
}
