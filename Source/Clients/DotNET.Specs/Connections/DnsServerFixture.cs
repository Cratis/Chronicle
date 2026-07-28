// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides a shared CoreDNS container for the chronicle+srv integration specs.
/// It runs the same <c>coredns/coredns:1.12.1</c> image and serves the exact same
/// <c>chronicle.local</c> zone the Composition sample uses, so the client's DNS SRV resolution
/// is exercised against the real records the scale-out story relies on.
/// </summary>
public sealed class DnsServerFixture : IAsyncLifetime
{
    const string Image = "coredns/coredns:1.12.1";
    const int DnsPort = 53;

    IContainer? _container;

    /// <summary>
    /// Gets the DNS server (host:port) to pass as the <c>srvNameServer</c> connection-string option.
    /// It targets the mapped UDP port of the CoreDNS container.
    /// </summary>
    public string SrvNameServer => $"127.0.0.1:{_container!.GetMappedPublicPort($"{DnsPort}/udp")}";

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder(Image)
            .WithResourceMapping(ReadDnsConfig("Corefile"), "/config/Corefile")
            .WithResourceMapping(ReadDnsConfig("chronicle.local.zone"), "/config/chronicle.local.zone")
            .WithCommand("-conf", "/config/Corefile")
            .WithPortBinding($"{DnsPort}/udp", assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("CoreDNS-"))
            .Build();

        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    static byte[] ReadDnsConfig(string fileName) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "dns", fileName));
}
