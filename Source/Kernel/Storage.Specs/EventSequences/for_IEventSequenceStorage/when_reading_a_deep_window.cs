// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_IEventSequenceStorage;

public abstract class when_reading_a_deep_window<THarness> : Specification
    where THarness : IEventSequenceWindowHarness, new()
{
    THarness _harness;
    IEventSequenceStorage _storage;
    readonly List<ulong> _numbers = [];
    int _batches;

    async Task Establish()
    {
        _harness = CreateHarness();
        _storage = await _harness.CreateStorage();
        var result = await _storage.AppendMany(Enumerable.Range(0, 10000).Select(number => new EventToAppendToStorage(
            (ulong)number,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            new EventType("selected", EventTypeGeneration.First),
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new ExpandoObject(),
            EventHash.NotSet)));
        result.IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        using var cursor = await _storage.GetWindow(0, 9950, 11, true);
        while (await cursor.MoveNext())
        {
            _batches++;
            _numbers.AddRange(cursor.Current.Select(@event => @event.Context.SequenceNumber.Value));
        }
    }

    void Destroy() => _harness?.Dispose();

    [Fact] public void should_materialize_only_the_requested_page_plus_one_lookahead() => _numbers.Count.ShouldEqual(11);
    [Fact] public void should_preserve_deep_descending_order() => _numbers.ShouldEqual(Enumerable.Range(9940, 11).Reverse().Select(number => (ulong)number));
    [Fact] public void should_not_continue_reading_batches_from_the_rest_of_the_sequence() => _batches.ShouldEqual(1);

    /// <summary>
    /// Creates the backend harness, overridable for infrastructure fixtures.
    /// </summary>
    /// <returns>The harness to run the contract against.</returns>
    protected virtual THarness CreateHarness() => new();
}
