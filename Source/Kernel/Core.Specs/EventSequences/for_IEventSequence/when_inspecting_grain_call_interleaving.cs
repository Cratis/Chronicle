// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

/// <summary>
/// The non-mutating sequence-number reads are served concurrently with an in-flight append instead of queueing behind
/// it - an append holds the grain for several storage round trips, and the observer paths poll these reads on every
/// watchdog tick and state transition. Appending stays non-interleaved: it mutates the sequence number and must remain
/// serialized.
/// </summary>
public class when_inspecting_grain_call_interleaving : Specification
{
    bool _nextSequenceNumberIsInterleaved;
    bool _tailSequenceNumberIsInterleaved;
    bool _tailSequenceNumberForEventTypesIsInterleaved;
    bool _appendIsInterleaved;

    void Because()
    {
        _nextSequenceNumberIsInterleaved = AllOverloadsInterleaved(nameof(IEventSequence.GetNextSequenceNumber));
        _tailSequenceNumberIsInterleaved = AllOverloadsInterleaved(nameof(IEventSequence.GetTailSequenceNumber));
        _tailSequenceNumberForEventTypesIsInterleaved = AllOverloadsInterleaved(nameof(IEventSequence.GetTailSequenceNumberForEventTypes));
        _appendIsInterleaved = AnyOverloadInterleaved(nameof(IEventSequence.Append));
    }

    [Fact] void should_interleave_getting_the_next_sequence_number() => _nextSequenceNumberIsInterleaved.ShouldBeTrue();
    [Fact] void should_interleave_getting_the_tail_sequence_number() => _tailSequenceNumberIsInterleaved.ShouldBeTrue();
    [Fact] void should_interleave_getting_the_tail_sequence_number_for_event_types() => _tailSequenceNumberForEventTypesIsInterleaved.ShouldBeTrue();
    [Fact] void should_not_interleave_appending() => _appendIsInterleaved.ShouldBeFalse();

    static MethodInfo[] OverloadsOf(string methodName)
    {
        var overloads = typeof(IEventSequence).GetMethods().Where(method => method.Name == methodName).ToArray();
        overloads.ShouldNotBeEmpty();
        return overloads;
    }

    static bool AllOverloadsInterleaved(string methodName) => Array.TrueForAll(OverloadsOf(methodName), IsInterleaved);

    static bool AnyOverloadInterleaved(string methodName) => Array.Exists(OverloadsOf(methodName), IsInterleaved);

    static bool IsInterleaved(MethodInfo method) => Attribute.IsDefined(method, typeof(AlwaysInterleaveAttribute));
}
