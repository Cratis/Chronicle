// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class a_conditional_replacement<THarness>(ulong? storedSequence, bool remove, IsolatedReplaceResult expectedResult, int? expectedCount) : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    IsolatedReplaceResult _result;
    ExpandoObject? _stored;
    int? _count;

    async Task Establish()
    {
        if (storedSequence is { } sequence)
        {
            await Seed(sequence);
        }
    }

    async Task Because()
    {
        _result = await _sink.ReplaceIsolated(_key, remove ? null : State(20), 20);
        _stored = await _sink.FindOrDefault(_key);
        _count = _stored is null ? null : await CurrentCount();
    }

    [Fact] public void should_report_the_conditional_outcome() => _result.ShouldEqual(expectedResult);
    [Fact] public void should_store_only_the_admitted_state() => _count.ShouldEqual(expectedCount);
    [Fact] public void should_advertise_support() => _sink.SupportsIsolatedReplace.ShouldBeTrue();
}
