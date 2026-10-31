// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LegacyEventsSource.when_lifting;

public class and_sequence_is_outside_source_events : Specification
{
    const string Declaration = """
        capture Shipments
          source api
            sequence inbox-api
          source events
            from ShipmentDispatched
          append OrderShipped
            sequence inbox-other
        """;
    LegacyEventsSource _result;

    void Because() => _result = LegacyEventsSource.Lift(Declaration);

    [Fact] void should_leave_the_source_unchanged() => _result.Source.ShouldEqual(Declaration);
    [Fact] void should_not_lift_a_sequence() => _result.Sequence.ShouldBeNull();
}
