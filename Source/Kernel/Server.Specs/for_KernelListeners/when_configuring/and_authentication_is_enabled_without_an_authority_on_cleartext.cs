// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_authentication_is_enabled_without_an_authority_on_cleartext : given.listeners
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => Configure(new() { Tls = new() { Enabled = false } }, null));

    [Fact] void should_fail_before_opening_the_listener() => _listeners.ShouldBeEmpty();
    [Fact] void should_require_an_explicit_https_authority() => _exception.Message.ShouldContain("Authentication:Authority");
}
