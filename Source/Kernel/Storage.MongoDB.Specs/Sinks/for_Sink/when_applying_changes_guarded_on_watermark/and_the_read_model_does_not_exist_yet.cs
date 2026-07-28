// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// A guarded write is an update only: a conditional upsert would raise a duplicate key error in exactly the
/// already-applied case and, inside an ordered bulk write, discard every operation queued behind it.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_read_model_does_not_exist_yet(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    ExpandoObject? _result;

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_not_create_the_read_model() => _result.ShouldBeNull();
}
