// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Workbench.for_WorkbenchListener;

public class when_an_ip_address_is_configured : Specification
{
    ListenOptions _listener;

    void Because()
    {
        var options = new KestrelServerOptions();
        options.ConfigureEndpointDefaults(listener => _listener = listener);
        WorkbenchListener.Configure(options, new ChronicleWorkbenchOptions { Address = "192.0.2.10", Port = 9877 });
    }

    [Fact] void should_listen_on_the_configured_address() => _listener.IPEndPoint.Address.ShouldEqual(IPAddress.Parse("192.0.2.10"));
    [Fact] void should_use_the_configured_port() => _listener.IPEndPoint.Port.ShouldEqual(9877);
}
