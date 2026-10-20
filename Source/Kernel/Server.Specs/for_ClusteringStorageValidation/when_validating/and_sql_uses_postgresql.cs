// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Server.for_ClusteringStorageValidation.when_validating;

public class and_sql_uses_postgresql : Specification
{
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => ClusteringStorageValidation.Validate(new ChronicleOptions
    {
        Clustering = new() { Type = ClusteringType.Sql },
        Storage = new() { Type = StorageType.PostgreSql.ToUpperInvariant() }
    }));

    [Fact] void should_accept_the_combination() => _exception.ShouldBeNull();
}
