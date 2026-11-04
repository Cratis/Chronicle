// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectionOwnerResolver.when_resolving;

public class with_authentication_disabled : Specification
{
    readonly ConnectionOwnerResolver _resolver = new(Options.Create(new ChronicleOptions { Authentication = new() { Enabled = false } }));
    ConnectionOwner _owner;

    void Because() => _owner = _resolver.Resolve(new ClaimsPrincipal());

    [Fact] void should_resolve_as_anonymous() => _owner.ShouldEqual(ConnectionOwner.Anonymous);
}
