// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_event_is_redacted : given.a_stored_event
{
    void Establish() => _stored = _stored with { Context = _stored.Context with { EventType = new(GlobalEventTypes.Redaction, EventTypeGeneration.First) } };

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_report_unavailable() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
