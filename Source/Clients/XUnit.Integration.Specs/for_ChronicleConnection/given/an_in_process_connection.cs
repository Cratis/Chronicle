// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Net;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;

public class an_in_process_connection : Specification
{
    private protected ChronicleConnection _connection;
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
        _connection = new ChronicleConnection(lifecycle, _grainFactory, _siloDetails, new KernelCore::Cratis.Chronicle.Clients.ConnectionOwnership(TimeProvider.System), NullLoggerFactory.Instance);
        _capabilities = _connection;

        lifecycle.OnConnected += () =>
        {
            _knownInCallback = _capabilities.CapabilitiesAreKnown;
            _capabilitiesInCallback = [.. _capabilities.Capabilities];
            return Task.CompletedTask;
        };
    }

    void Destroy() => _connection.Dispose();
}
