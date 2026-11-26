// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Workbench.for_WorkbenchListener;

public class when_localhost_is_configured : Specification
{
    ListenOptions _listener;

    void Because()
    {
        var options = new KestrelServerOptions();
        options.ConfigureEndpointDefaults(listener => _listener = listener);
        WorkbenchListener.Configure(options, new ChronicleWorkbenchOptions { Address = "localhost", Port = 9877 });
    }

    [Fact] void should_use_the_dual_loopback_listener() => _listener.GetType().Name.ShouldEqual("LocalhostListenOptions");
    [Fact] void should_use_the_configured_port() => _listener.IPEndPoint.Port.ShouldEqual(9877);
}
