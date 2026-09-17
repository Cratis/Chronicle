// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Services.Host;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_checking_compatibility;

/// <summary>
/// The client sent the exact same descriptor set the server itself ships - trivially compatible regardless of
/// which one is treated as older. This exercises the wiring between <see cref="ConnectionService"/> and
/// <see cref="Compatibility.ConnectCompatibility"/> - whether the response is populated at all, and from the
/// right values - not the direction logic itself, which <c language="csharp">for_ConnectCompatibility</c>
/// already covers on its own. Before this suite, <see cref="ConnectionService.CheckCompatibility"/> - the
/// exact method that refused #4058's connection - had no specification at all.
/// </summary>
public class and_the_client_sent_the_servers_own_descriptor_set : Specification
{
    IConnectionService _connectionService;
    CompatibilityResponse _result;

    void Establish() =>
        _connectionService = new KernelConnectionService(
            Substitute.For<IGrainFactory>(),
            Substitute.For<ILocalSiloDetails>(),
            new ConnectedClientsQuery(Substitute.For<IGrainFactory>(), Options.Create(new ChronicleOptions())),
            NullLogger<KernelConnectionService>.Instance,
            Options.Create(new ChronicleOptions()));

    async Task Because() => _result = await _connectionService.CheckCompatibility(new CompatibilityRequest
    {
        ClientType = ".NET",
        ClientVersion = "1.0.0",
        ProtocolVersion = "0.0.1",
        DescriptorSet = Contracts.WireContractDescriptorSet.Bytes.ToArray()
    });

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Incompatibilities.ShouldBeEmpty();
    [Fact] void should_report_the_running_server_version() => _result.ServerVersion.ShouldEqual(ServerVersion.Version);
    [Fact] void should_report_the_servers_protocol_version() => _result.ServerProtocolVersion.ShouldEqual(Contracts.ProtocolVersion.Current);
}
