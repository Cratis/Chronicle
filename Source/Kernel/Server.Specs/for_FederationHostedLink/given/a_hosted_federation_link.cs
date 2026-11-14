// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Federation;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Server;
using AcceptorFederation = Cratis.Chronicle.Configuration.Federation;
using FederatedEvent = Cratis.Chronicle.Federation.FederatedEvent;
using FederationDirection = Cratis.Chronicle.Federation.FederationDirection;
using FederationPolicyStatus = Cratis.Chronicle.Federation.FederationPolicyStatus;

namespace Cratis.Chronicle.Server.for_FederationHostedLink.given;

/// <summary>
/// Two kernels over real TLS 1.3 on the loopback interface. The operator kernel listens with a certificate its own authority issued and
/// admits a peer by chain, registry and policy. The customer kernel only dials, with a certificate its authority issued, and one
/// connection carries events in both directions.
/// </summary>
/// <remarks>
/// Everything between the sockets is the production code: the listener's TLS settings, the caller, the admission, the pull service, the serve
/// service and the session grain's rules, and on the dialing side the connection factory, the link pump and the serve client. Only storage is
/// replaced: the outbox of each kernel is a list, and each inbox records what was appended to it.
/// </remarks>
public class a_hosted_federation_link : Specification
{
    protected const string Subject = "acme";
    protected const string OperatorStore = "operator";
    protected const string OperatorNamespace = "Default";
    protected const string CustomerStore = "customer";
    protected const string EventTypeId = "type-a";

    protected static readonly FederationConsumerTiming Timing = new(
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromSeconds(10));

    protected List<(string Origin, ulong SequenceNumber, string Content)> _operatorInbox = [];
    protected List<(string Origin, ulong SequenceNumber, string Content)> _customerInbox = [];
    protected List<(string Store, string Namespace)> _operatorInboxTargets = [];
    protected List<FederatedEvent> _operatorOutbox = [];
    protected List<FederatedEvent> _customerOutbox = [];
    protected FederationPolicyState _policy;
    protected bool _certificateRevoked;
    protected FederationLinkPump _pump;
    protected FederationServeClient _serveClient;
    protected int _connections;
    protected string _failure;

    static readonly Lock _gate = new();
    readonly List<ConnectionContext> _accepted = [];
    X509Certificate2 _authority;
    X509Certificate2 _customerCertificate;
    ECDsa _customerKey;
    WebApplication _app;
    CancellationTokenSource _cancellation;

    protected static bool Skip => !for_FederationEnrollmentEndpoints.given.Tls13.Supported;

    async Task Establish()
    {
        if (Skip)
        {
            return;
        }

        _cancellation = new(TimeSpan.FromSeconds(60));
        var port = FreePort();
        var endpoint = new Uri($"https://localhost:{port}/");
        _authority = FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var operatorOptions = Options.Create(new ChronicleOptions
        {
            Federation = new AcceptorFederation
            {
                Enabled = true,
                Port = port,
                Hosted = new() { Enabled = true, PublicEndpoint = endpoint.ToString(), TrustDomain = "example" }
            }
        });

        _customerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var csr = new CertificateRequest("CN=peer", _customerKey, HashAlgorithmName.SHA256).CreateSigningRequest();
        var issued = FederationCertificateAuthority.IssueClient(
            _authority,
            csr,
            $"spiffe://example/customer/{Subject}/kernel/prod",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(14));
        _customerCertificate = issued.CopyWithPrivateKey(_customerKey);
        var serial = issued.SerialNumber;
        var identity = FederationCertificateAuthority.GetIdentity(issued)!;

        _policy = new(
            Subject,
            FederationPolicyStatus.Active,
            [
                new(FederationDirection.AcceptorOutbound, OperatorStore, OperatorNamespace, true, []),
                new(FederationDirection.AcceptorInbound, OperatorStore, OperatorNamespace, true, [])
            ],
            "operator");

        var keys = Substitute.For<IFederationAuthorityKeys>();
        keys.GetOrCreate().Returns((_authority, false));

        var registry = Substitute.For<IFederationCertificateRegistry>();
        registry.GetAuthority().Returns(_ => Task.FromResult<FederationAuthorityCreated?>(new("fingerprint", "example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1))));
        registry.FindIssuedCertificate(serial).Returns(_ => Task.FromResult<FederationIssuedCertificateState?>(
            new(serial, "enrollment", Subject, "prod", identity, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(14), FederationIssuanceReasons.Enrollment, _certificateRevoked)));
        registry.FindPolicy(Subject).Returns(_ => Task.FromResult<FederationPolicyState?>(_policy));

        var listenerCertificate = new FederationListenerCertificate(keys, operatorOptions, TimeProvider.System);
        await listenerCertificate.Refresh();

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var admission = new FederationPeerAdmission(registry, keys, operatorOptions, TimeProvider.System);

        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCodeFirstGrpc();
        builder.Services.AddSingleton(operatorOptions);
        builder.Services.AddSingleton<IFederationCaller, HttpFederationCaller>();
        builder.Services.AddSingleton<IFederationPullService>(provider => CreateOperatorPullService(operatorOptions, admission, meterFactory, provider.GetRequiredService<IFederationCaller>()));
        builder.Services.AddSingleton<IFederationServeService>(provider => CreateOperatorServeService(operatorOptions, admission, meterFactory, provider.GetRequiredService<IFederationCaller>()));
        builder.Services.AddSingleton<TestFederationPull>();
        builder.Services.AddSingleton<Cratis.Chronicle.Services.Federation.FederationServe>();
        builder.Services.AddSingleton(Substitute.For<IFederationStatus>());
        builder.Services.AddSingleton(Substitute.For<IFederationRegistry>());
        builder.WebHost.UseKestrel(kestrel =>
            kestrel.Listen(IPAddress.Loopback, port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.Use(next => async context =>
                {
                    lock (_accepted)
                    {
                        _accepted.Add(context);
                        _connections++;
                    }

                    await next(context);
                });
                listenOptions.UseHttps(new TlsHandshakeCallbackOptions
                {
                    OnConnection = _ => ValueTask.FromResult(new System.Net.Security.SslServerAuthenticationOptions
                    {
                        ServerCertificateContext = listenerCertificate.Current,
#pragma warning disable CA5398 // Federation deliberately accepts TLS 1.3 only.
                        EnabledSslProtocols = SslProtocols.Tls13,
#pragma warning restore CA5398
                        ClientCertificateRequired = true,
#pragma warning disable CA5359 // As in the kernel: identity is decided by the service.
                        RemoteCertificateValidationCallback = static (_, _, _, _) => true,
#pragma warning restore CA5359
                        ApplicationProtocols = [System.Net.Security.SslApplicationProtocol.Http2]
                    })
                });
            }));
        _app = builder.Build();
        _app.MapGrpcService<TestFederationPull>();
        _app.MapGrpcService<Cratis.Chronicle.Services.Federation.FederationServe>();
        await _app.StartAsync();

        CreateCustomerKernel(endpoint, identity, serial);
    }

    async Task Destroy()
    {
        _pump?.Dispose();
        _serveClient?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _customerCertificate?.Dispose();
        _customerKey?.Dispose();
        _authority?.Dispose();
        _cancellation?.Dispose();
    }

    protected static FederatedEvent Event(ulong sequenceNumber) => new(
        sequenceNumber,
        EventTypeId,
        1,
        "Type",
        $"source-{sequenceNumber}",
        "Stream",
        "stream",
        DateTimeOffset.UtcNow,
        Guid.NewGuid().ToString(),
        string.Empty,
        $"{{\"value\":{sequenceNumber}}}",
        []);

    protected static void Add(List<FederatedEvent> outbox, params ulong[] sequenceNumbers)
    {
        lock (_gate)
        {
            outbox.AddRange(sequenceNumbers.Select(Event));
        }
    }

    protected int Connections()
    {
        lock (_accepted)
        {
            return _connections;
        }
    }

    protected void AbortConnections()
    {
        lock (_accepted)
        {
            foreach (var connection in _accepted)
            {
                connection.Abort();
            }

            _accepted.Clear();
        }
    }

    /// <summary>
    /// Drives the dialer's link, the way the consumer grain's timer does, until the condition holds or the time is up.
    /// </summary>
    /// <param name="condition">What to wait for.</param>
    /// <param name="timeout">How long to wait at most.</param>
    /// <returns>True when the condition held.</returns>
    protected async Task<bool> Drive(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (!condition() && DateTime.UtcNow < deadline)
        {
            var delay = await _pump.Step(_cancellation.Token) ?? TimeSpan.FromMilliseconds(50);
            await Task.Delay(delay > TimeSpan.FromMilliseconds(50) ? TimeSpan.FromMilliseconds(50) : delay, _cancellation.Token);
        }

        return condition();
    }

    /// <summary>
    /// Drives the dialer's link for a while, to show that nothing more arrives.
    /// </summary>
    /// <param name="time">How long to drive it.</param>
    /// <returns>Awaitable task.</returns>
    protected Task Settle(TimeSpan time) => Drive(() => false, time);

    protected int Count(List<(string Origin, ulong SequenceNumber, string Content)> inbox)
    {
        lock (_gate)
        {
            return inbox.Count;
        }
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    FederationPullService CreateOperatorPullService(IOptions<ChronicleOptions> options, IFederationPeerAdmission admission, IMeterFactory meterFactory, IFederationCaller caller)
    {
        var outbox = Substitute.For<IEventSequenceStorage>();
        outbox.GetTailSequenceNumber().Returns(_ =>
        {
            lock (_gate)
            {
                return Task.FromResult(_operatorOutbox.Count == 0 ? EventSequenceNumber.Unavailable : new EventSequenceNumber(_operatorOutbox.Max(e => e.SequenceNumber)));
            }
        });
        outbox.GetRange(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId?>(),
            Arg.Any<IEnumerable<EventType>?>(),
            Arg.Any<IEnumerable<Concepts.Events.Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(callInfo =>
            {
                var start = callInfo.ArgAt<EventSequenceNumber>(0);
                var end = callInfo.ArgAt<EventSequenceNumber>(1);
                var cursor = Substitute.For<IEventCursor>();
                var delivered = false;
                cursor.MoveNext().Returns(_ =>
                {
                    var more = !delivered;
                    delivered = true;
                    return Task.FromResult(more);
                });
                cursor.Current.Returns(_ =>
                {
                    lock (_gate)
                    {
                        return _operatorOutbox
                            .Where(e => e.SequenceNumber >= start.Value && e.SequenceNumber <= end.Value)
                            .OrderBy(e => e.SequenceNumber)
                            .Select(e => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType(EventTypeId, 1), e.SequenceNumber))
                            .ToArray();
                    }
                });
                return Task.FromResult(cursor);
            });

        var eventTypes = Substitute.For<IEventTypesStorage>();
        var schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "value": { "type": "integer" } } }""");
        var eventType = new EventType(EventTypeId, 1);
        eventTypes.HasFor(eventType.Id, Arg.Any<EventTypeGeneration?>()).Returns(true);
        eventTypes.GetFor(eventType.Id, Arg.Any<EventTypeGeneration?>()).Returns(new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, schema, EventTypeVisibility.Public));

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        eventStoreStorage.EventTypes.Returns(eventTypes);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        var converter = Substitute.For<IExpandoObjectConverter>();
        converter.ToJsonObject(Arg.Any<System.Dynamic.ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject { ["value"] = 1 });

        return new FederationPullService(
            caller,
            admission,
            Substitute.For<IFederationRegistry>(),
            Substitute.For<IFederationStatus>(),
            storage,
            converter,
            new FederationRateLimiter(TimeProvider.System),
            new FederationMetrics(meterFactory),
            options,
            NullLogger<FederationPullService>.Instance);
    }

    FederationServeService CreateOperatorServeService(IOptions<ChronicleOptions> options, IFederationPeerAdmission admission, IMeterFactory meterFactory, IFederationCaller caller)
    {
        var inbox = Substitute.For<IFederationInbox>();
        inbox.GetResumePoint(Arg.Any<FederationBindingState>()).Returns(_ =>
        {
            lock (_gate)
            {
                return Task.FromResult<FederationResumePoint?>(_operatorInbox.Count == 0 ? null : new FederationResumePoint(_operatorInbox[^1].Origin, _operatorInbox[^1].SequenceNumber));
            }
        });
        inbox.Append(Arg.Any<FederationBindingState>(), Arg.Any<string>(), Arg.Any<Cratis.Chronicle.Federation.FederatedEvent>()).Returns(call =>
        {
            lock (_gate)
            {
                _operatorInbox.Add((call.ArgAt<string>(1), call.ArgAt<Cratis.Chronicle.Federation.FederatedEvent>(2).SequenceNumber, call.ArgAt<Cratis.Chronicle.Federation.FederatedEvent>(2).Content));
                _operatorInboxTargets.Add((call.ArgAt<FederationBindingState>(0).TargetEventStore, call.ArgAt<FederationBindingState>(0).TargetNamespace));
            }

            return Task.FromResult<string?>(null);
        });

        var subscriptions = Substitute.For<IFederationSubscriptions>();
        subscriptions.Get(Arg.Any<FederationBindingState>()).Returns(Task.FromResult<IReadOnlyList<FederationSubscription>>([new([EventTypeId], [])]));

        var sessions = new Dictionary<string, FederationSession>();
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IFederationSession>(Arg.Any<string>(), Arg.Any<string?>()).Returns(call =>
        {
            lock (sessions)
            {
                var key = call.ArgAt<string>(0);
                if (!sessions.TryGetValue(key, out var session))
                {
                    sessions[key] = session = new();
                }

                return session;
            }
        });

        return new FederationServeService(
            caller,
            admission,
            inbox,
            subscriptions,
            Substitute.For<IFederationStatus>(),
            new FederationRateLimiter(TimeProvider.System),
            new FederationMetrics(meterFactory),
            Substitute.For<IFederationConsumerMetrics>(),
            grains,
            options,
            NullLogger<FederationServeService>.Instance,
            Timing);
    }

    void CreateCustomerKernel(Uri endpoint, string identity, string serial)
    {
        var dial = new FederationDialOptions { Enabled = true, EventStore = CustomerStore, Namespace = "Default", LogicalSource = "platform", Serve = true };
        var customerOptions = Options.Create(new ChronicleOptions { Federation = new AcceptorFederation { Enabled = false, Dial = dial } });

        var identities = Substitute.For<IFederationEnrolledIdentities>();
        identities.Get().Returns(_ => Task.FromResult<FederationEnrolledIdentity?>(new(
            _customerCertificate,
            X509CertificateLoader.LoadCertificate(_authority.RawData),
            identity,
            serial,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(14),
            endpoint.ToString(),
            FederationCertificateAuthority.Fingerprint(_authority))));

        var binding = FederationDial.Binding(dial)!;
        var registry = Substitute.For<IFederationRegistry>();
        registry.GetBindings().Returns(_ => Task.FromResult<IReadOnlyList<FederationBindingState>>([binding]));

        var inbox = Substitute.For<IFederationInbox>();
        inbox.GetResumePoint(Arg.Any<FederationBindingState>()).Returns(_ =>
        {
            lock (_gate)
            {
                return Task.FromResult<FederationResumePoint?>(_customerInbox.Count == 0 ? null : new FederationResumePoint(_customerInbox[^1].Origin, _customerInbox[^1].SequenceNumber));
            }
        });
        inbox.Append(Arg.Any<FederationBindingState>(), Arg.Any<string>(), Arg.Any<Cratis.Chronicle.Federation.FederatedEvent>()).Returns(call =>
        {
            lock (_gate)
            {
                _customerInbox.Add((call.ArgAt<string>(1), call.ArgAt<Cratis.Chronicle.Federation.FederatedEvent>(2).SequenceNumber, call.ArgAt<Cratis.Chronicle.Federation.FederatedEvent>(2).Content));
            }

            return Task.FromResult<string?>(null);
        });

        var subscriptions = Substitute.For<IFederationSubscriptions>();
        subscriptions.Get(Arg.Any<FederationBindingState>()).Returns(Task.FromResult<IReadOnlyList<FederationSubscription>>([new([EventTypeId], [])]));

        var outboxReader = Substitute.For<IFederationPullService>();
        outboxReader
            .Read(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ulong>(), Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IReadOnlyList<FederationGrant>>())
            .Returns(call =>
            {
                var from = call.ArgAt<ulong>(3);
                FederatedEvent[] events;
                lock (_gate)
                {
                    events = [.. _customerOutbox.Where(e => e.SequenceNumber >= from).OrderBy(e => e.SequenceNumber).Take(call.ArgAt<int>(4))];
                }

                var tail = events.Length == 0 ? from : events[^1].SequenceNumber;
                return Task.FromResult(new FederatedEventBatch(FederatedEventBatchStatus.Ok, CustomerStore, events, events.Length == 0 ? from : tail + 1, tail));
            });

        _serveClient = new(outboxReader, customerOptions, NullLogger<FederationServeClient>.Instance, Timing);
        _pump = new(
            binding.Id,
            registry,
            Substitute.For<IFederationStatus>(),
            Substitute.For<IFederationPullClientFactory>(),
            inbox,
            subscriptions,
            Substitute.For<IFederationConsumerMetrics>(),
            customerOptions,
            NullLogger<FederationLinkPump>.Instance,
            Timing,
            dialClients: new FederationDialClientFactory(identities, TimeProvider.System),
            serveClient: _serveClient);
    }

    /// <summary>
    /// The pull service as the kernel exposes it, over the pull service of the operator kernel.
    /// </summary>
    /// <param name="service">The pull service.</param>
    public sealed class TestFederationPull(IFederationPullService service) : IFederationPull
    {
        /// <inheritdoc/>
        public async Task<QueryResult<FederatedEventBatchResponse>> Pull(PullRequest request, CallContext callContext = default)
        {
            var batch = await service.Pull(request.EventStore, request.Namespace, request.FromSequenceNumber, request.Limit, request.EventTypes);
            return QueryResult<FederatedEventBatchResponse>.Success(Guid.NewGuid(), new FederatedEventBatchResponse
            {
                Status = batch.Status,
                OriginEventStore = batch.OriginEventStore,
                Events = [.. batch.Events.Select(_ => _.ToContract())],
                NextSequenceNumber = batch.NextSequenceNumber,
                TailSequenceNumber = batch.TailSequenceNumber
            });
        }
    }
}
