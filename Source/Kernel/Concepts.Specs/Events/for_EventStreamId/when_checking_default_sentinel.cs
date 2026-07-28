// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventStreamId;

public class when_checking_default_sentinel : Specification
{
    [Fact] void should_report_the_default_value_as_default() => new EventStreamId(EventStreamId.Default).IsDefault.ShouldBeTrue();
    [Fact] void should_not_report_a_named_stream_as_default() => new EventStreamId("Monthly").IsDefault.ShouldBeFalse();
    [Fact] void should_not_report_an_empty_stream_id_as_default() => new EventStreamId(string.Empty).IsDefault.ShouldBeFalse();
}
