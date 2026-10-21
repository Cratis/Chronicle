// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Server.for_ClusteringStorageValidation.when_validating;

public class and_mongodb_uses_mssql : Specification
{
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => ClusteringStorageValidation.Validate(new ChronicleOptions
    {
        Clustering = new() { Type = ClusteringType.MongoDB },
        Storage = new() { Type = StorageType.MsSql.ToUpperInvariant() }
    }));

    [Fact] void should_reject_the_combination() => _exception.ShouldBeOfExactType<MongoDBClusteringRequiresMongoDBStorage>();
}
