// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_the_explicit_authority_is_not_https : given.listeners
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => Configure(
        new()
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Authority = "http://login.example.com" }
        },
        null));

    [Fact] void should_reject_the_authority() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_open_a_cleartext_listener() => _listeners.ShouldBeEmpty();
}
