// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendError;

public class when_checking_stream_completed_sentinel : Specification
{
    [Fact] void should_have_a_descriptive_message() => AppendError.StreamCompleted.Value.ShouldEqual("Cannot append to a stream that has been completed.");
    [Fact] void should_not_be_the_unknown_sentinel() => AppendError.StreamCompleted.ShouldNotEqual(AppendError.Unknown);
}
