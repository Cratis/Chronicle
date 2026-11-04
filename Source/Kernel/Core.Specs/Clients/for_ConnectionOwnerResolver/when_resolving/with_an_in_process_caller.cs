// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectionOwnerResolver.when_resolving;

public class with_an_in_process_caller : Specification
{
    readonly ConnectionOwnerResolver _resolver = new(Options.Create(new ChronicleOptions()));
    ConnectionOwner _owner;

    void Because() => _owner = _resolver.Current();

    [Fact] void should_resolve_the_internal_owner() => _owner.ShouldEqual(ConnectionOwner.InProcess);
}
