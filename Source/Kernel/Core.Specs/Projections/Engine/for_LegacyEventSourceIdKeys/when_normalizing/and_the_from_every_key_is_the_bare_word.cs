// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_the_from_every_key_is_the_bare_word : given.a_projection_definition
{
    void Establish() => _definition = Definition(fromEveryKey: "EventSourceId");

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_leave_the_every_event_key_unchanged() => _result.FromEvery.Key.Value.ShouldEqual("EventSourceId");
}
