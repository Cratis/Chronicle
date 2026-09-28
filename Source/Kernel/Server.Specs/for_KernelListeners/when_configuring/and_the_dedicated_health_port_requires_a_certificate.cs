// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_the_dedicated_health_port_requires_a_certificate : given.listeners
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => Configure(
        new()
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Enabled = false },
            Health = new() { Port = 8080 }
        },
        null));

    [Fact] void should_fail_before_opening_either_listener() => _listeners.ShouldBeEmpty();
    [Fact] void should_explain_the_health_port_certificate_requirement() => _exception.Message.ShouldContain("Health:Tls");
}
