// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectionOwnerResolver.when_resolving;

public class with_a_null_network_identity : Specification
{
    readonly ConnectionOwnerResolver _resolver = new(Options.Create(new ChronicleOptions()));
    ConnectionOwner _owner;

    void Because() => _owner = _resolver.Resolve(null, hasTransport: true);

    [Fact] void should_deny_missing_network_identity() => _owner.ShouldEqual(ConnectionOwner.Unidentified);
}
