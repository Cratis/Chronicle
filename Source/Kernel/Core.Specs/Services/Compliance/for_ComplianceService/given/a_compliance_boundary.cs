// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.given;

public class a_compliance_boundary : Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given.a_pii_manager
{
    protected static readonly EventStoreName ThirdEventStore = "third-event-store";
    protected IAuthorizationDecisions _decisions;
    protected IJsonSchemaMetadataManager _schema;
    protected IGrainFactory _grainFactory;
    protected DecisionLogger _audit;
    protected ClaimsPrincipal _principal;
    protected ServiceProvider _services;
    IServiceScope? _requestScope;
    protected CallContext _context;
    private protected ComplianceService _service;
    protected List<EventStoreName> _keys;
    protected bool _allDecidedBeforeMutation;

    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "compliance-caller", ClaimValueTypes.String, "test-issuer")], "test"));
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Allow(AuthorizationReason.Granted, call.Arg<Capability>(), call.Arg<AuthorizationScope>()));
        _schema = Substitute.For<IJsonSchemaMetadataManager>();
        _schema.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => call.Arg<JsonObject>());
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IPIIManager>(Guid.Empty, new PIIManagerKey(EventStore, EventStoreNamespace)).Returns(_manager);
        _audit = new DecisionLogger();
        _services = new ServiceCollection().AddScoped(_ => _decisions).BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _storage.GetEventStores().Returns([EventStore, OtherEventStore, ThirdEventStore, EventStoreName.System, (EventStoreName)"system", OtherEventStore]);
        _keys = [EventStore, OtherEventStore, ThirdEventStore, EventStoreName.System];
        _keyStore.When(store => store.DeleteFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Identifier))
            .Do(call => _keys.Remove(call.Arg<EventStoreName>()));
        var observedDecisions = _decisions;
        _keyStore.When(store => store.RecordErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Identifier))
            .Do(_ => _allDecidedBeforeMutation = observedDecisions.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAuthorizationDecisions.Decide)) == 3);
        UseCaller(_principal);
        Configure(enforce: true);
    }

    protected void Configure(bool enforce, bool authenticationEnabled = true)
    {
        var options = Options.Create(new ChronicleOptions { Authentication = new() { Enabled = authenticationEnabled, EnforceComplianceCapabilities = enforce } });
        var authorization = new ComplianceAuthorization(options, _storage, _services.GetRequiredService<IServiceScopeFactory>(), _audit);
        _service = new ComplianceService(_grainFactory, _schema, NullLogger<ComplianceService>.Instance, authorization);
    }

    protected void UseCaller(ClaimsPrincipal principal)
    {
        _requestScope?.Dispose();
        _requestScope = _services.CreateScope();
        var httpContext = new DefaultHttpContext { User = principal, RequestServices = _requestScope.ServiceProvider };
        _context = new CallContext(new object(), new Cratis.Chronicle.Services.for_QueryExecutor.FakeServerCallContext(httpContext));
    }

    protected void Deny(EventStoreName eventStore, bool throws = false) =>
        _decisions.Decide(
                Arg.Any<ClaimsPrincipal?>(),
                WellKnownCapabilities.ManageComplianceKeys,
                Arg.Is<AuthorizationScope>(scope => scope.EventStore == eventStore))
            .Returns(call => throws
                ? Task.FromException<AuthorizationDecision>(new InvalidOperationException("sensitive lookup detail"))
                : Task.FromResult(AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.Arg<Capability>(), call.Arg<AuthorizationScope>())));

    protected async Task<string[]> Operate(bool allowNew)
    {
        if (allowNew)
        {
            var result = await _service.AllowNewEncryptionKeyWithCoverage(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context);
            return result.NotCoveredEventStores.ToArray();
        }

        var deleted = await _service.DeleteEncryptionKeyWithCoverage(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context);
        return deleted.NotCoveredEventStores.ToArray();
    }

    protected ReleaseRequest ReleaseRequest(bool strict) => new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        Subject = Identifier,
        Schema = "{}",
        Payload = "{}",
        Strict = strict
    };

    protected void ShouldNotHaveTouched(EventStoreName eventStore)
    {
        _keyStore.DidNotReceive().RecordErasureFor(eventStore, Arg.Any<EventStoreNamespaceName>(), Identifier);
        _keyStore.DidNotReceive().DeleteFor(eventStore, Arg.Any<EventStoreNamespaceName>(), Identifier);
        _keyStore.DidNotReceive().AllowNewKeyFor(eventStore, Arg.Any<EventStoreNamespaceName>(), Identifier);
        _cacheClient.DidNotReceive().Evict(eventStore, Arg.Any<EventStoreNamespaceName>(), Identifier);
    }

    void Destroy()
    {
        _requestScope?.Dispose();
        _services.Dispose();
    }
}
