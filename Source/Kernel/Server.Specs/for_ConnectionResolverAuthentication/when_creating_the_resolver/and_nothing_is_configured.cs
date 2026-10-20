// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_ConnectionResolverAuthentication.when_creating_the_resolver;

/// <summary>
/// Without a connectionResolution section, registering the resolver authentication changes nothing: connections
/// stay as connectionDetails describes them.
/// </summary>
public class and_nothing_is_configured : given.a_configured_storage_connection_resolution
{
    ResolvedConnection _resolved;

    void Establish() => Configure(new Dictionary<string, string?>());

    async Task Because() => _resolved = await _resolution.Resolve(ConnectionPurpose.Runtime, StorageConnectionKind.Namespace, "orders", "acme");

    [Fact] void should_not_be_configured() => _resolution.IsConfigured.ShouldBeFalse();
    [Fact] void should_leave_the_connection_unchanged() => _resolved.ShouldEqual(ResolvedConnection.Unchanged);
    [Fact] void should_not_ask_any_provider_for_tokens() => _recordingProvider.DidNotReceiveWithAnyArgs().CanProvide(default!);
}
