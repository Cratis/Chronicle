// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration.for_StorageConnectionResolution.when_resolving;

public class a_namespace : given.a_storage_connection_resolution
{
    ResolvedConnection _result;

    void Establish() => _options.Connection.UseHttp(new Uri("https://platform.example/connections"));

    async Task Because() => _result = await _resolution.Resolve(ConnectionPurpose.Schema, StorageConnectionKind.Namespace, "orders", "acme");

    [Fact] void should_resolve_for_the_storage() => _request!.Storage.ShouldEqual(WellKnownConnectionStorage.PostgreSql);
    [Fact] void should_resolve_for_the_purpose() => _request!.Purpose.ShouldEqual(ConnectionPurpose.Schema);
    [Fact] void should_resolve_the_namespace_kind() => _request!.Scope[StorageConnectionRequests.Kind].ShouldEqual("namespace");
    [Fact] void should_resolve_for_the_event_store() => _request!.Scope[StorageConnectionRequests.EventStore].ShouldEqual("orders");
    [Fact] void should_resolve_for_the_namespace() => _request!.Scope[StorageConnectionRequests.Namespace].ShouldEqual("acme");
    [Fact] void should_keep_the_resolved_connection_string() => _result.ConnectionString.ShouldEqual("Server=placed");
    [Fact] void should_keep_the_resolved_database() => _result.Database.ShouldEqual("placed");
}
