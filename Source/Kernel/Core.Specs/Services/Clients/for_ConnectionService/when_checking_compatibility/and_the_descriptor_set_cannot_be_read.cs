// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_checking_compatibility;

/// <summary>
/// A descriptor set that will not parse says nothing about whether the two sides agree - refusing the
/// connection over it would turn a malformed payload from one client into an outage for it. It is reported as
/// an incompatibility instead, so the client can say something useful and decide for itself.
/// </summary>
public class and_the_descriptor_set_cannot_be_read : Specification
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
        ProtocolVersion = "1.0.0",
        DescriptorSet = [1, 2, 3]
    });

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_say_the_descriptor_set_could_not_be_read() =>
        _result.Incompatibilities.ShouldContain(_ => _.Contains("could not be read", StringComparison.Ordinal));
}
