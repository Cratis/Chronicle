// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes;

public class when_migrate_is_selected : Specification
{
    VerifyMongoDBIndexes _task;
    int _inspections;

    void Establish() => _task = new(
        Options.Create(new ChronicleOptions()),
        _ =>
        {
            _inspections++;
            return Task.FromResult(new MongoDBIndexReport([]));
        },
        NullLogger<VerifyMongoDBIndexes>.Instance);

    Task Because() => _task.Execute(CancellationToken.None);

    [Fact] void should_not_inspect_startup_scopes() => _inspections.ShouldEqual(0);
}
