// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.given;

public class a_link_pump : Specification
{
    protected const string BindingId = "binding";
    protected const string LocalEventStore = "local";

    protected FederationBindingState _binding;
    protected IFederationRegistry _registry;
    protected FakeStatus _status;
    protected FakePull _pull;
    protected FakePullClients _clients;
    protected FakeInbox _inbox;
    protected FakeSubscriptions _subscriptions;
    protected IFederationConsumerMetrics _metrics;
    protected IOptions<ChronicleOptions> _options;
    protected FederationLinkPump _pump;
    protected FederationConsumerTiming _timing;

    void Establish()
    {
        _binding = CreateBinding(BindingId);
        _registry = Substitute.For<IFederationRegistry>();
        _registry.GetBindings().Returns(_ => Task.FromResult<IReadOnlyList<FederationBindingState>>([_binding]));
        _status = new();
        _pull = new();
        _clients = new(_pull);
        _inbox = new();
        _subscriptions = new();
        _subscriptions.Subscriptions.Add(new(["type-a", "type-b"], []));
        _metrics = Substitute.For<IFederationConsumerMetrics>();
        _options = Options.Create(new ChronicleOptions { Federation = new Cratis.Chronicle.Configuration.Federation { Enabled = true } });
        _timing = FederationConsumerTiming.Default;
        _pump = CreatePump(BindingId, _pull, _clients, _inbox);
    }

    void Destroy() => _pump.Dispose();

    protected static FederationBindingState CreateBinding(string id) => new(
        id,
        "logical-source",
        "https://producer.example:35100",
        "sha256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
        "client",
        "remote",
        "RemoteNamespace",
        LocalEventStore,
        "Default",
        FederationOrigin.Runtime);

    protected FederationLinkPump CreatePump(string id, FakePull pull, FakePullClients clients, FakeInbox inbox) => new(
        id,
        _registry,
        _status,
        clients,
        inbox,
        _subscriptions,
        _metrics,
        _options,
        NullLogger<FederationLinkPump>.Instance,
        _timing);
}
