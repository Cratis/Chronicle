// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Json.for_JsonElementExtensions;

public class when_trying_to_get_value_that_holds_datetimeoffset : when_trying_to_get_value_of_type<DateTimeOffset>
{
    protected override DateTimeOffset expected => DateTimeOffset.Parse(DateTimeOffset.UtcNow.ToString());

    [Fact] void should_be_able_to_get_value() => result.ShouldBeTrue();
    [Fact] void should_get_expected_value() => output.ShouldEqual(expected);
}
