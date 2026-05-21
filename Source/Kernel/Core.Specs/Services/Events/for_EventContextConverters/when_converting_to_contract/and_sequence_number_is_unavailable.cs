// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using static Cratis.Chronicle.Services.TypeScriptSequenceNumberCompatibility;

namespace Cratis.Chronicle.Services.Events.for_EventContextConverters.when_converting_to_contract;

public class and_sequence_number_is_unavailable : Specification
{
    Contracts.Events.EventContext _result;

    void Because() => _result = EventContext.Empty.ToContract();

    [Fact] void should_sanitize_sequence_number() => _result.SequenceNumber.ShouldEqual(MaxSafeInteger);
}
