// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A link pump of the dialer link of a dial-only kernel, whose one connection is also what it serves its own outbox over.
/// </summary>
public class a_dialing_link_pump : a_link_pump
{
    protected FakeServe _serve;
    protected FakeDialClients _dialClients;
    protected IFederationPullService _pullService;
    protected FederationServeClient _serveClient;
    protected FederationLinkPump _dialPump;

    void Establish()
    {
        _binding = FederationDial.Binding(new FederationDialOptions { Enabled = true, EventStore = LocalEventStore, LogicalSource = "logical-source" })!;
        _options = Options.Create(new ChronicleOptions
        {
            Federation = new Cratis.Chronicle.Configuration.Federation
            {
                Dial = new FederationDialOptions { Enabled = true, EventStore = LocalEventStore, LogicalSource = "logical-source", Serve = true }
            }
        });

        _serve = new();
        _dialClients = new(_pull, _serve);
        _pullService = Substitute.For<IFederationPullService>();
        _pullService
            .Read(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ulong>(), Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IReadOnlyList<FederationGrant>>())
            .Returns(call => Task.FromResult(new FederatedEventBatch(FederatedEventBatchStatus.Ok, LocalEventStore, [], call.ArgAt<ulong>(3), 0)));
        _serveClient = new(_pullService, _options, NullLogger<FederationServeClient>.Instance, _timing);
        _dialPump = new(
            _binding.Id,
            _registry,
            _status,
            _clients,
            _inbox,
            _subscriptions,
            _metrics,
            _options,
            NullLogger<FederationLinkPump>.Instance,
            _timing,
            dialClients: _dialClients,
            serveClient: _serveClient);
    }

    void Destroy()
    {
        _dialPump.Dispose();
        _serveClient.Dispose();
    }
}
