// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_property_dictionaries_are_ordered_differently : given.a_definition
{
    void Establish()
    {
        var first = _definition.From[_first] with { Properties = new Dictionary<PropertyPath, string> { ["a"] = "a", ["b"] = "b" } };
        _definition = _definition with { From = new Dictionary<EventType, FromDefinition> { [_first] = first } };
        _other = _definition with { From = new Dictionary<EventType, FromDefinition> { [_first] = first with { Properties = new Dictionary<PropertyPath, string> { ["b"] = "b", ["a"] = "a" } } } };
    }
    void Because() => Compute();
    [Fact] void should_keep_the_digest() => _otherDigest.ShouldEqual(_digest);
}
