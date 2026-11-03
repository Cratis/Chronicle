// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_array_order_differs : given.a_definition
{
    void Establish()
    {
        _definition = _definition with { Tags = ["first", "second"] };
        _other = _definition with { Tags = ["second", "first"] };
    }
    void Because() => Compute();
    [Fact] void should_change_the_digest() => _otherDigest.ShouldNotEqual(_digest);
}
