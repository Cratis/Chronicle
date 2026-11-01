// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

using System.Net;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ConnectionService = KernelGrpc::Cratis.Chronicle.Services.Clients.ConnectionService;
using OpenConnections = KernelGrpc::Cratis.Chronicle.Services.Clients.OpenConnections;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;

public class an_in_process_connection : Specification
{
    protected ChronicleConnection _connection;
    protected IKernelCapabilities _capabilities;
    protected IGrainFactory _grainFactory;
    protected ILocalSiloDetails _siloDetails;
    protected bool _knownInCallback;
    protected string[] _capabilitiesInCallback = [];

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _siloDetails = Substitute.For<ILocalSiloDetails>();
        _siloDetails.SiloAddress.Returns(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 0));

        var lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
        _connection = new ChronicleConnection(lifecycle, _grainFactory, _siloDetails, NullLoggerFactory.Instance);
        _capabilities = _connection;

        lifecycle.OnConnected += () =>
        {
            _knownInCallback = _capabilities.CapabilitiesAreKnown;
            _capabilitiesInCallback = [.. _capabilities.Capabilities];
            return Task.CompletedTask;
        };
    }

    void Destroy() => _connection.Dispose();

    protected Task<CompatibilityResponse> CheckKernelCompatibility()
    {
        var options = Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions());
        var service = new ConnectionService(
            _grainFactory,
            _siloDetails,
            new KernelCore::Cratis.Chronicle.Clients.ConnectedClientsQuery(_grainFactory, options),
            new OpenConnections(),
            NullLogger<ConnectionService>.Instance,
            options);
        return service.CheckCompatibility(new CompatibilityRequest
        {
            ClientType = ".NET",
            ClientVersion = "0.0.0",
            ProtocolVersion = Contracts.ProtocolVersion.Current,
            DescriptorSet = Contracts.WireContractDescriptorSet.Bytes.ToArray()
        });
    }
}
