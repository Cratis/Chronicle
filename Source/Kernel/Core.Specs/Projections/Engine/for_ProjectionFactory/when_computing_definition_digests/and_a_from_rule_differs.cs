// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_a_from_rule_differs : given.a_definition
{
    void Establish() => _other = _definition with { From = new Dictionary<EventType, FromDefinition> { [_first] = _definition.From[_first] with { Properties = new Dictionary<PropertyPath, string> { ["name"] = "different" } }, [_second] = _definition.From[_second] } };
    void Because() => Compute();
    [Fact] void should_change_the_digest() => _otherDigest.ShouldNotEqual(_digest);
}
