// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration.for_StorageConnectionResolution.when_resolving;

public class the_cluster : given.a_storage_connection_resolution
{
    ResolvedConnection _result;

    void Establish() => _options.Identity.UseHttp(new Uri("https://platform.example/connections"));

    async Task Because() => _result = await _resolution.Resolve(ConnectionPurpose.Runtime, StorageConnectionKind.Cluster);

    [Fact] void should_resolve_the_cluster_kind() => _request!.Scope[StorageConnectionRequests.Kind].ShouldEqual("cluster");
    [Fact] void should_keep_the_configured_connection_string() => _result.ConnectionString.ShouldBeNull();
    [Fact] void should_keep_the_configured_database() => _result.Database.ShouldBeNull();
    [Fact] void should_resolve_the_schema() => _result.Schema.ShouldEqual("placed_schema");
    [Fact] void should_resolve_the_identity() => _result.Identity!.AccessToken.ShouldEqual("token");
}
