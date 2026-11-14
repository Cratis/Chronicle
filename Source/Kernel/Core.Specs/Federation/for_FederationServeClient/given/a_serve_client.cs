// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Federation.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationServeClient.given;

/// <summary>
/// The dialer's side of producing, over a fake connection to an acceptor.
/// </summary>
public class a_serve_client : Specification
{
    protected static readonly FederationConsumerTiming Timing = new(
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(5),
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(30),
        TimeSpan.FromSeconds(2));

    protected TimerClock _clock;
    protected FakeServe _serve;
    protected FakeDialClients _connections;
    protected IFederationPullService _pullService;
    protected FederationServeClient _client;
    protected IFederationPullConnection _connection;
    protected bool _serving = true;
    protected string[] _serveEventTypes = [];
    protected List<(string Store, string Namespace, ulong From, int Limit, string[] Types, IReadOnlyList<FederationGrant> Grants)> _reads;

    async Task Establish()
    {
        _reads = [];
        _clock = new();
        _serve = new();
        _connections = new(new FakePull(), _serve);
        _pullService = Substitute.For<IFederationPullService>();
        _pullService
            .Read(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ulong>(), Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IReadOnlyList<FederationGrant>>())
            .Returns(call =>
            {
                _reads.Add((call.ArgAt<string>(1), call.ArgAt<string>(2), call.ArgAt<ulong>(3), call.ArgAt<int>(4), [.. call.ArgAt<IEnumerable<string>>(5)], call.ArgAt<IReadOnlyList<FederationGrant>>(6)));
                return Task.FromResult(new FederatedEventBatch(FederatedEventBatchStatus.Ok, "local", [], call.ArgAt<ulong>(3), 0));
            });
        _connection = await _connections.Create(null!);
    }

    void Destroy()
    {
        _client?.Dispose();
        _connection?.Dispose();
    }

    protected void Start()
    {
        var options = Options.Create(new ChronicleOptions
        {
            Federation = new Cratis.Chronicle.Configuration.Federation
            {
                Dial = new FederationDialOptions { Enabled = true, EventStore = "local", Namespace = "ns", Serve = _serving, ServeEventTypes = _serveEventTypes }
            }
        });
        _client = new(_pullService, options, NullLogger<FederationServeClient>.Instance, Timing, _clock);
        _client.Start(_connection);
    }
}
