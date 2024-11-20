// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.EventSequences.for_AppendEventResult;

public class when_result_is_duplicate_event_sequence_number : Specification
{
    static AppendEventResult result;

    void Because() => result = AppendEventError.DuplicateEventSequenceNumber;

    [Fact] void should_not_be_successful() => result.IsSuccess.ShouldBeFalse();
}
