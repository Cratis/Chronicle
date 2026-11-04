// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectionOwnerResolver.when_resolving;

public class with_a_null_network_identity_in_the_holder : Specification
{
    readonly ConnectionOwnerResolver _resolver = new(Options.Create(new ChronicleOptions()));
    ConnectionOwner _owner;

    void Because()
    {
        _resolver.HasTransport = true;
        _owner = _resolver.Current();
    }

    [Fact] void should_not_treat_the_call_as_direct() => _owner.ShouldEqual(ConnectionOwner.Unidentified);
}
