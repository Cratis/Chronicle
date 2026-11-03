// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_IEventSequenceStorage;

public abstract class when_reading_unsigned_window_bounds<THarness> : Specification
    where THarness : IEventSequenceWindowHarness, new()
{
    const ulong Ceiling = long.MaxValue;
    readonly Dictionary<(ulong From, ulong Until, bool Descending), ulong[]> _pages = [];
    THarness _harness;
    IEventSequenceStorage _storage;

    async Task Establish()
    {
        _harness = CreateHarness();
        _storage = await _harness.CreateStorage();
        foreach (var number in (ulong[])[2, 0, Ceiling, Ceiling - 1])
        {
            var result = await _storage.AppendMany([new EventToAppendToStorage(
                number,
                "source-type",
                "source",
                EventStreamType.All,
                EventStreamId.Default,
                new EventType("event", EventTypeGeneration.First),
                CorrelationId.NotSet,
                [],
                [],
                [],
                DateTimeOffset.UtcNow,
                new ExpandoObject(),
                EventHash.NotSet)]);
            result.IsSuccess.ShouldBeTrue();
        }
    }

    async Task Because()
    {
        foreach (var descending in (bool[])[false, true])
        {
            foreach (var bound in (ulong[])[Ceiling + 1, ulong.MaxValue - 2, ulong.MaxValue - 1, ulong.MaxValue])
            {
                await Read(bound, EventSequenceNumber.Max.Value, descending);
                await Read(0, bound, descending);
            }

            await Read(Ceiling, Ceiling, descending);
            await Read(Ceiling - 1, Ceiling, descending);
            await Read(Ceiling, Ceiling - 1, descending);
        }
    }

    void Destroy() => _harness?.Dispose();

    [Theory]
    [InlineData(Ceiling + 1)]
    [InlineData(ulong.MaxValue - 2)]
    [InlineData(ulong.MaxValue - 1)]
    [InlineData(ulong.MaxValue)]
    public void should_return_empty_for_a_lower_bound_beyond_the_storage_ceiling(ulong from)
    {
        _pages[(from, EventSequenceNumber.Max.Value, false)].ShouldBeEmpty();
        _pages[(from, EventSequenceNumber.Max.Value, true)].ShouldBeEmpty();
    }

    [Theory]
    [InlineData(Ceiling + 1)]
    [InlineData(ulong.MaxValue - 2)]
    [InlineData(ulong.MaxValue - 1)]
    [InlineData(ulong.MaxValue)]
    public void should_treat_a_high_upper_bound_as_unbounded_without_wrapping(ulong until)
    {
        _pages[(0, until, false)].ShouldEqual([0UL, 2UL, Ceiling - 1, Ceiling]);
        _pages[(0, until, true)].ShouldEqual([Ceiling, Ceiling - 1, 2UL, 0UL]);
    }

    [Fact] public void should_include_the_ceiling_in_an_ascending_window() => _pages[(Ceiling, Ceiling, false)].ShouldEqual([Ceiling]);
    [Fact] public void should_include_the_ceiling_in_a_descending_window() => _pages[(Ceiling, Ceiling, true)].ShouldEqual([Ceiling]);
    [Fact] public void should_preserve_ascending_order_near_the_ceiling() => _pages[(Ceiling - 1, Ceiling, false)].ShouldEqual([Ceiling - 1, Ceiling]);
    [Fact] public void should_preserve_descending_order_near_the_ceiling() => _pages[(Ceiling - 1, Ceiling, true)].ShouldEqual([Ceiling, Ceiling - 1]);
    [Fact] public void should_return_empty_for_a_reversed_ascending_range() => _pages[(Ceiling, Ceiling - 1, false)].ShouldBeEmpty();
    [Fact] public void should_return_empty_for_a_reversed_descending_range() => _pages[(Ceiling, Ceiling - 1, true)].ShouldBeEmpty();

    /// <summary>
    /// Creates the backend harness, overridable for infrastructure fixtures.
    /// </summary>
    /// <returns>The harness to run the contract against.</returns>
    protected virtual THarness CreateHarness() => new();

    async Task Read(ulong from, ulong until, bool descending)
    {
        using var cursor = await _storage.GetWindow(from, until, 4, descending);
        var numbers = new List<ulong>();
        while (await cursor.MoveNext())
        {
            numbers.AddRange(cursor.Current.Select(@event => @event.Context.SequenceNumber.Value));
        }

        _pages[(from, until, descending)] = numbers.ToArray();
    }
}
