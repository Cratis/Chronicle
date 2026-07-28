// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Connections.for_ChronicleServerAddressResolver.when_resolving_srv_records_from_a_running_dns_server.context;

namespace Cratis.Chronicle.Connections.for_ChronicleServerAddressResolver;

[Collection(DnsServerCollection.Name)]
public class when_resolving_srv_records_from_a_running_dns_server(context ctx) : IClassFixture<context>
{
    public class context(DnsServerFixture fixture) : IAsyncLifetime
    {
        public IReadOnlyList<ChronicleServerAddress> Result = default!;

        public async Task InitializeAsync()
        {
            // The default resolver builds its own LookupClient targeting the srvNameServer, so this
            // exercises the full chronicle+srv resolution path end to end - exactly what a client does.
            // The Composition zone serves two _chronicle._tcp.chronicle.local records - localhost:35001
            // and localhost:35002 - so resolving it should yield exactly those two cluster nodes.
            var connectionString = new ChronicleConnectionString($"chronicle+srv://chronicle.local/?srvNameServer={fixture.SrvNameServer}");
            Result = await new ChronicleServerAddressResolver().Resolve(connectionString);
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_discover_both_cluster_nodes() => ctx.Result.Count.ShouldEqual(2);
    [Fact] void should_discover_the_first_kernel_node() => ctx.Result.ShouldContain(new ChronicleServerAddress("localhost", 35001));
    [Fact] void should_discover_the_second_kernel_node() => ctx.Result.ShouldContain(new ChronicleServerAddress("localhost", 35002));
}
