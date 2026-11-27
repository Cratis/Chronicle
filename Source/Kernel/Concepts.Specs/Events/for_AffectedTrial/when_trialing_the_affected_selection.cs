// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_AffectedTrial;

public class when_trialing_the_affected_selection : Specification
{
    [Fact] void should_fail_on_purpose() => 1.ShouldEqual(2);
}
