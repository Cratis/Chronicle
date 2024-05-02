// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_JsonElementExtensions;

public class when_trying_to_get_value_that_holds_int64 : when_trying_to_get_value_of_type<long>
{
    protected override long expected => long.MaxValue;

    [Fact] void should_be_able_to_get_value() => result.ShouldBeTrue();
    [Fact] void should_get_expected_value() => output.ShouldEqual(expected);
}
