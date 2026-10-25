// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

/// <summary>
/// Rejects unresolved keys rather than allowing multiple read models to share an unaddressable row.
/// </summary>
/// <typeparam name="THarness">The sink implementation under specification.</typeparam>
public abstract class and_the_key_has_no_value<THarness> : given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    Exception? _exception;

    async Task Because() => _exception = await Catch.Exception(() =>
        _sink.ApplyChanges(new Key(null!, ArrayIndexers.NoIndexers), ChangesetSettingCountTo(1), 1UL));

    [Fact] public void should_reject_the_unresolved_key() => _exception.ShouldBeOfExactType<ReadModelKeyHasNoValue>();
    [Fact] public void should_identify_the_read_model() => _exception!.Message.ShouldContain("test-read-model");
}
