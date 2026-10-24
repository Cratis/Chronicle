// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationInitializationStorage.when_recording_twice;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : given.a_sqlserver_initialization_storage(fixture)
{
    bool _first;
    bool _second;
    GenerationInitializationReceipt _persisted;

    async Task Because()
    {
        _first = await _audit.TryRecord(Receipt());
        _second = await _audit.TryRecord(Receipt("replacement"));
        _persisted = (await _audit.GetReceipts()).Single();
    }
    [Fact] void should_record_first() => _first.ShouldBeTrue();
    [Fact] void should_refuse_replacement() => _second.ShouldBeFalse();
    [Fact] void should_preserve_original() => _persisted.Evidence.ShouldEqual("original");
}
