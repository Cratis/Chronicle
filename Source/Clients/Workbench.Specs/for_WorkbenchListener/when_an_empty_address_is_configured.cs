// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Workbench.for_WorkbenchListener;

public class when_an_empty_address_is_configured : Specification
{
    ListenOptions _listener;

    void Because()
    {
        var options = new KestrelServerOptions();
        options.ConfigureEndpointDefaults(listener => _listener = listener);
        WorkbenchListener.Configure(options, new ChronicleWorkbenchOptions { Address = string.Empty });
    }

    [Fact] void should_listen_on_all_interfaces() => _listener.IPEndPoint.Address.ShouldEqual(IPAddress.IPv6Any);
}
