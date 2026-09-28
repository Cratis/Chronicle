// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_tls_is_enabled_without_a_certificate : given.listeners
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => Configure(new(), null));

    [Fact] void should_fail_startup() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_register_a_cleartext_listener() => _listeners.ShouldBeEmpty();
}
