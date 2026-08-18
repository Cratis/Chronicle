// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Events.for_ConstraintType;

/// <summary>
/// These numbers reach the wire. The constraint type is converted to its contracts counterpart by number, and every
/// client - .NET, Kotlin, TypeScript, Elixir - decodes it as an enum value, so renumbering one here silently
/// re-labels violations that are already stored and already in flight.
/// </summary>
/// <remarks>
/// Pinning them is what makes that a failing specification rather than a support case. Adding a value is safe and
/// needs a line here; changing one is not.
/// </remarks>
public class when_reading_the_declared_values : Specification
{
    [Fact] void should_number_unknown_zero() => ((int)ConstraintType.Unknown).ShouldEqual(0);
    [Fact] void should_number_unique_one() => ((int)ConstraintType.Unique).ShouldEqual(1);
    [Fact] void should_number_unique_event_type_two() => ((int)ConstraintType.UniqueEventType).ShouldEqual(2);
    [Fact] void should_number_schema_three() => ((int)ConstraintType.Schema).ShouldEqual(3);
    [Fact] void should_number_stream_closed_four() => ((int)ConstraintType.StreamClosed).ShouldEqual(4);

    [Fact]
    void should_declare_nothing_else() =>
        Enum.GetValues<ConstraintType>().Length.ShouldEqual(5);
}
