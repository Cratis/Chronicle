// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// A guarded write is an update only. Pinning it here keeps the in-memory sink from diverging from the persistent
/// ones, where a conditional upsert would surface the already-applied case as a duplicate key error.
/// </summary>
public class and_the_read_model_does_not_exist_yet : given.an_accumulating_read_model
{
    ExpandoObject? _result;

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_not_create_the_read_model() => _result.ShouldBeNull();
}
