// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration.for_StorageConnectionResolution.when_resolving;

public class and_nothing_is_configured : given.a_storage_connection_resolution
{
    ResolvedConnection _result;

    async Task Because() => _result = await _resolution.Resolve(ConnectionPurpose.Runtime, StorageConnectionKind.Namespace, "orders", "acme");

    [Fact] void should_keep_the_configured_connection() => _result.ShouldEqual(ResolvedConnection.Unchanged);
    [Fact] void should_not_create_a_resolver() => _factory.DidNotReceive().Create(Arg.Any<ConnectionResolutionOptions>());
}
