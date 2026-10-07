// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Primitives;

using context = Cratis.Chronicle.Integration.for_Reactors.when_a_reactor_registration_is_rejected.and_keep_alives_are_sent_on_the_same_channel.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_a_reactor_registration_is_rejected;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/4537: a rejected reactor registration left the kernel
/// reading the reactor call's request stream after the call had ended, and a unary call that reused the same HTTP/2
/// stream - the connection keep-alive - failed with "Reading is already in progress".
/// </summary>
/// <remarks>
/// The transport is only present out of process: there the context opens its own client to the kernel container,
/// so every call below shares one gRPC channel and one HTTP/2 connection. In process the calls go straight to the
/// kernel services. Keep-alives are sent continuously while the reactor calls fail, so new streams arrive on the
/// connection the moment a failed reactor call hands its stream back.
/// </remarks>
/// <param name="context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class and_keep_alives_are_sent_on_the_same_channel(context context) : Given<context>(context)
{
    /// <summary>
    /// The number of rounds of concurrent reactor calls. Kestrel keeps a stream the server ended while the client still had
    /// its request stream open for a few seconds, and refuses new streams (ENHANCE_YOUR_CALM) once a connection tracks more
    /// than 200 - so the total stays well below that.
    /// </summary>
    public const int Rounds = 20;
    public const int ConcurrentReactorCalls = 4;
    public const int KeepAliveSenders = 8;

    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        static readonly TimeSpan _reactorCallTimeout = TimeSpan.FromSeconds(30);

        public ConcurrentBag<Exception?> ReactorCallOutcomes = [];
        public ConcurrentBag<Exception> KeepAliveFailures = [];
        public int KeepAlivesSucceeded;

        async Task Because()
        {
            using var dedicatedClient = CreateClientForOutOfProcessKernel();
            var connection = dedicatedClient is null
                ? EventStore.Connection
                : (await dedicatedClient.GetEventStore(EventStore.Name)).Connection;
            var services = ((IChronicleServicesAccessor)connection).Services;
            var connectionId = connection.Lifecycle.ConnectionId.ToString();

            using var stopSending = new CancellationTokenSource();
            var senders = Enumerable.Range(0, KeepAliveSenders)
                .Select(_ => Task.Run(() => SendKeepAlivesUntilStopped(services, connectionId, stopSending.Token)))
                .ToArray();

            for (var round = 0; round < Rounds; round++)
            {
                var calls = Enumerable.Range(0, ConcurrentReactorCalls)
                    .Select(_ => ObserveWithInvalidRegistration(services, connectionId));
                foreach (var outcome in await Task.WhenAll(calls))
                {
                    ReactorCallOutcomes.Add(outcome);
                }
            }

            await stopSending.CancelAsync();
            await Task.WhenAll(senders);

            // A final keep-alive after every reactor call has ended must still go through.
            await SendKeepAlive(services, connectionId);
        }

        async Task SendKeepAlivesUntilStopped(IServices services, string connectionId, CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                await SendKeepAlive(services, connectionId);
            }
        }

        async Task SendKeepAlive(IServices services, string connectionId)
        {
            try
            {
                await services.Connections.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = connectionId });
                Interlocked.Increment(ref KeepAlivesSucceeded);
            }
            catch (Exception ex)
            {
                KeepAliveFailures.Add(ex);
            }
        }

        async Task<Exception?> ObserveWithInvalidRegistration(IServices services, string connectionId)
        {
            // Replaying, so the registration is not lost if it is sent before the transport subscribes to the request stream.
            // The request stream is deliberately never completed, as a client that has not yet given up on the call would.
            using var messages = new ReplaySubject<ReactorMessage>();
            var ended = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var subscription = services.Reactors.Observe(messages).Subscribe(
                _ => { },
                error => ended.TrySetResult(error),
                () => ended.TrySetResult(null));

            messages.OnNext(new ReactorMessage(new OneOf<RegisterReactor, ReactorResult>(new RegisterReactor
            {
                ConnectionId = connectionId,
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                Reactor = new ReactorDefinition
                {
                    ReactorId = $"invalid-reactor-{Guid.NewGuid():N}",
                    EventSequenceId = EventSequences.EventSequenceId.Log.Value,
                    EventTypes = [new EventTypeWithKeyExpression { EventType = null!, Key = "$eventSourceId" }]
                }
            })));

            try
            {
                return await ended.Task.WaitAsync(_reactorCallTimeout);
            }
            catch (TimeoutException)
            {
                // Surface the kernel's failures; the container logs are otherwise lost when it is torn down.
                var kernelLogs = ChronicleFixture is ChronicleConfigurableFixture configurable
                    ? await configurable.GetOutOfProcessKernelLogs()
                    : string.Empty;
                var kernelFailures = kernelLogs
                    .Split('\n')
                    .Where(line => line.Contains("Error when executing service method", StringComparison.Ordinal) || line.Contains("InvalidOperationException", StringComparison.Ordinal));
                throw new TimeoutException($"A reactor call with an invalid registration did not end within {_reactorCallTimeout}.\n{string.Join('\n', kernelFailures)}");
            }
        }

        ChronicleClient? CreateClientForOutOfProcessKernel()
        {
            if (ChronicleFixture is not ChronicleFixture configurable || configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess)
            {
                return null;
            }

            // skipTlsValidation because the container serves a self-signed development certificate.
            return new ChronicleClient(new ChronicleClientOptions
            {
                ConnectionString = new ChronicleConnectionString($"chronicle://localhost:{configurable.KernelGrpcHostPort}/?skipTlsValidation=true"),
                EventStore = EventStore.Name,
                AutoDiscoverAndRegister = false
            });
        }
    }

    [Fact] void should_end_every_reactor_call() => Context.ReactorCallOutcomes.Count.ShouldEqual(Rounds * ConcurrentReactorCalls);
    [Fact] void should_end_every_reactor_call_with_an_error() => Context.ReactorCallOutcomes.All(outcome => outcome is not null).ShouldBeTrue();
    [Fact] void should_not_fail_any_keep_alive() => string.Join(Environment.NewLine, Context.KeepAliveFailures.Select(_ => _.Message)).ShouldEqual(string.Empty);
    [Fact] void should_send_keep_alives() => Context.KeepAlivesSucceeded.ShouldBeGreaterThan(0);
}
