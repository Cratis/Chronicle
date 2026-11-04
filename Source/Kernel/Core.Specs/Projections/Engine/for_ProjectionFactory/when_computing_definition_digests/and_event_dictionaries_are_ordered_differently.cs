// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_event_dictionaries_are_ordered_differently : given.a_definition
{
    void Establish() => _other = _definition with { From = new Dictionary<EventType, FromDefinition> { [_second] = _definition.From[_second], [_first] = _definition.From[_first] } };
    void Because() => Compute();
    [Fact] void should_keep_the_digest() => _otherDigest.ShouldEqual(_digest);
}
