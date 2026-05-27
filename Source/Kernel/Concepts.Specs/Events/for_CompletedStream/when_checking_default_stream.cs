// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_CompletedStream;

public class when_checking_default_stream : Specification
{
    [Fact] void should_report_default_stream_pair_as_default() =>
        new CompletedStream(EventStreamType.All, EventStreamId.Default).IsDefault.ShouldBeTrue();

    [Fact] void should_report_non_default_stream_type_as_not_default() =>
        new CompletedStream("Bookings", EventStreamId.Default).IsDefault.ShouldBeFalse();

    [Fact] void should_report_non_default_stream_id_as_not_default() =>
        new CompletedStream(EventStreamType.All, "January").IsDefault.ShouldBeFalse();

    [Fact] void should_report_fully_custom_stream_as_not_default() =>
        new CompletedStream("Bookings", "January").IsDefault.ShouldBeFalse();
}
