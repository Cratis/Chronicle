// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Cluster;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache;

public class when_alternating_databases_and_purposes : given.a_cache
{
    string _first;
    string _result;

    void Establish() => _first = Normalize(_input);

    void Because()
    {
        _cache.Get(new(DatabaseKind.Namespace, "store", "other"), ConnectionPurpose.Runtime, _input with { Schema = "other" });
        _cache.Get(_identity, ConnectionPurpose.Schema, _input with { Schema = "metadata" });
        _result = Normalize(_input);
    }

    [Fact] void should_keep_the_normalization_for_each_database_and_purpose() => ReferenceEquals(_first, _result).ShouldBeTrue();
}
