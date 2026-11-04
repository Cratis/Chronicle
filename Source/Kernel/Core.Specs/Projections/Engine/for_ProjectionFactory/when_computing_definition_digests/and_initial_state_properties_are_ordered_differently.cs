// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_initial_state_properties_are_ordered_differently : given.a_definition
{
    void Establish()
    {
        _definition = _definition with { InitialModelState = new JsonObject { ["a"] = 1, ["b"] = 2 } };
        _other = _definition with { InitialModelState = new JsonObject { ["b"] = 2, ["a"] = 1 } };
    }
    void Because() => Compute();
    [Fact] void should_keep_the_digest() => _otherDigest.ShouldEqual(_digest);
}
