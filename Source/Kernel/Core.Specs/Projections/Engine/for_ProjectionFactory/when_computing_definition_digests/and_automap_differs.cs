// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_automap_differs : given.a_definition
{
    void Establish() => _other = _definition with { AutoMap = AutoMap.Enabled };
    void Because() => Compute();
    [Fact] void should_change_the_digest() => _otherDigest.ShouldNotEqual(_digest);
}
