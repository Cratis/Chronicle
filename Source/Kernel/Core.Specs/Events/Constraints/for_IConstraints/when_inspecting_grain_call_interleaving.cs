// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Chronicle.Events.Constraints.for_IConstraints;

/// <summary>
/// A registration refreshes the constraints of the event sequences it reindexes, and those sequences read the version
/// and the definitions back - as does any sequence appending meanwhile. The reads therefore interleave with the
/// registration, or the two would wait on each other. Registering itself stays serialized.
/// </summary>
public class when_inspecting_grain_call_interleaving : Specification
{
    [Fact] void should_interleave_getting_the_version() => IsInterleaved(nameof(IConstraints.GetVersion)).ShouldBeTrue();
    [Fact] void should_interleave_getting_the_definitions() => IsInterleaved(nameof(IConstraints.GetDefinitions)).ShouldBeTrue();
    [Fact] void should_not_interleave_registering() => IsInterleaved(nameof(IConstraints.Register)).ShouldBeFalse();

    static bool IsInterleaved(string methodName) => Attribute.IsDefined(typeof(IConstraints).GetMethod(methodName)!, typeof(AlwaysInterleaveAttribute));
}
