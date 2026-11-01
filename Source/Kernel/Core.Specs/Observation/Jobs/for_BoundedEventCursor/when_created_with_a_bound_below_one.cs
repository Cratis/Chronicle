// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.Jobs.for_BoundedEventCursor;

public class when_created_with_a_bound_below_one : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _ = new BoundedEventCursor(Substitute.For<IEventCursor>(), 0));

    [Fact] void should_throw_argument_out_of_range() => _result.ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
