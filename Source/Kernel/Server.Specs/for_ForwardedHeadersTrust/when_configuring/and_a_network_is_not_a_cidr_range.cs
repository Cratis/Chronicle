// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_configuring;

public class and_a_network_is_not_a_cidr_range : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ForwardedHeadersTrust.Create(new()
    {
        ForwardedHeaders = new() { KnownNetworks = ["192.0.2.10"] }
    }));

    [Fact] void should_reject_the_invalid_configuration() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
