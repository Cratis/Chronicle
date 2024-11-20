// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_AppendEventResult;

public class when_result_is_appended_event : Specification
{

    static AppendEventResult result;

    void Because() => result = AppendedEvent.EmptyWithEventSequenceNumber(2);

    [Fact] void should_be_successful() => result.IsSuccess.ShouldBeTrue();
}
