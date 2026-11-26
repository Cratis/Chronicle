// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Assertions;

namespace Cratis.Chronicle.Testing.Reactors;

public partial class ReactorScenario<TReactor>
    where TReactor : class
{
    /// <summary>
    /// Asserts the complete cumulative recorded event list in order, ignoring destinations and commands.
    /// </summary>
    /// <param name="events">The typed event payloads in expected order.</param>
    /// <exception cref="ReactorSideEffectAssertionException">The recorded event count, type, or content differs.</exception>
    public void ShouldHaveProducedEventsInOrder(params object[] events)
    {
        var actual = ProducedEvents.Select(produced => produced.Event).ToArray();
        for (var index = 0; index < Math.Max(events.Length, actual.Length); index++)
        {
            if (index >= events.Length) throw OrderedFailure(index, $"expected <missing> but recorded {ExpectedValueComparer.DescribePayload(actual[index])}");
            if (index >= actual.Length) throw OrderedFailure(index, $"expected {ExpectedValueComparer.DescribePayload(events[index])} but recorded <missing>");
            var difference = ExpectedValueComparer.Compare(events[index], actual[index], ValueComparison.Typed);
            if (difference is not null) throw OrderedFailure(index, difference.ToString());
        }
    }

    /// <summary>
    /// Asserts the complete cumulative event multiset, ignoring order but preserving duplicate counts.
    /// </summary>
    /// <param name="events">The typed event payloads, without destinations or commands.</param>
    /// <exception cref="ReactorSideEffectAssertionException">Events are missing or unexpected.</exception>
    public void ShouldHaveProducedEventsInAnyOrder(params object[] events)
    {
        var unmatched = ProducedEvents.Select(produced => produced.Event).ToList();
        var missing = new List<object>();
        foreach (var expected in events)
        {
            var index = unmatched.FindIndex(actual => ExpectedValueComparer.Compare(expected, actual, ValueComparison.Typed) is null);
            if (index < 0) missing.Add(expected);
            else unmatched.RemoveAt(index);
        }
        if (missing.Count > 0 || unmatched.Count > 0)
        {
            throw new ReactorSideEffectAssertionException(
                $"Expected reactor '{typeof(TReactor).Name}' to produce exactly {events.Length} events, but recorded {ProducedEvents.Count}. " +
                $"Missing ({missing.Count}): {DescribeEvents(missing)}. Unexpected ({unmatched.Count}): {DescribeEvents(unmatched)}. {FirstDifference(missing, unmatched)}");
        }
    }

    /// <summary>
    /// Asserts that no events were recorded, irrespective of returned commands or unrecorded custom handlers.
    /// </summary>
    /// <exception cref="ReactorSideEffectAssertionException">Events were recorded.</exception>
    public void ShouldNotHaveProducedEvents() => ShouldHaveProducedEventsInOrder();

    static ReactorSideEffectAssertionException OrderedFailure(int index, string difference) =>
        new($"Expected reactor '{typeof(TReactor).Name}' event at index {index} to match, but {difference}.");

    static string DescribeEvents(IEnumerable<object> events)
    {
        var descriptions = events.Select(ExpectedValueComparer.DescribePayload).ToArray();

        return descriptions.Length == 0 ? "nothing" : string.Join(", ", descriptions);
    }

    static string FirstDifference(List<object> missing, List<object> unexpected)
    {
        if (missing.Count == 0 || unexpected.Count == 0) return string.Empty;

        return $"First unmatched payload difference: {ExpectedValueComparer.Compare(missing[0], unexpected[0], ValueComparison.Typed)}.";
    }
}
