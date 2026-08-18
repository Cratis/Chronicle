// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// A guarded write is an update only. Pinning it here keeps the implementations from diverging, since a
/// conditional upsert would surface the already-applied case as a duplicate key error instead.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_read_model_does_not_exist_yet<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    ExpandoObject? _result;

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] public void should_not_create_the_read_model() => _result.ShouldBeNull();
}
