// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectionOwnerResolver.when_resolving;

public class with_an_unidentified_network_caller : Specification
{
    readonly ConnectionOwnerResolver _resolver = new(Options.Create(new ChronicleOptions()));
    ConnectionOwner _owner;

    void Because() => _owner = _resolver.Resolve(new ClaimsPrincipal());

    [Fact] void should_not_resolve_as_in_process() => _owner.ShouldEqual(ConnectionOwner.Unidentified);
}
