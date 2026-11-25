// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Connections;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

public class and_only_the_identity_rotates : given.a_database_for_target_resolution
{
    SqlTarget _first;
    SqlTarget _result;

    async Task Because()
    {
        _first = await _target(_identity, ConnectionPurpose.Runtime, false);
        _resolved = new(Database: "tenant", Schema: "initial", Identity: new(AccessToken: "rotated-token"));
        _result = await _target(_identity, ConnectionPurpose.Runtime, false);
    }

    [Fact] void should_reuse_the_normalized_string() => ReferenceEquals(_first.ConnectionString, _result.ConnectionString).ShouldBeTrue();
    [Fact] void should_still_resolve_the_current_identity() => _resolution.Received(2).Resolve(ConnectionPurpose.Runtime, StorageConnectionKind.Namespace, "store", "tenant");
    [Fact] void should_keep_the_request_for_identity_application() => _result.Request.ShouldNotBeNull();
}
