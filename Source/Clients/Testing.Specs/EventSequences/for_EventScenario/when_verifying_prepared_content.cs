// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_verifying_prepared_content : Specification
{
    EventScenario _scenario;
    PreparedEvent _prepared;
    EventSourceId _source;
    AppendResult _append;
    ContentVerificationResult _result;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _source = EventSourceId.New();
        _prepared = await _scenario.EventLog.Prepare(new ContactReclassifiedV1("private", "public"));
        _append = await _scenario.EventLog.AppendPrepared(_source, _prepared);
        _append.ShouldBeSuccessful();
    }

    async Task Because() => _result = await _scenario.EventLog.VerifyContent(_append.SequenceNumber, _prepared, _source);

    [Fact] void should_verify_pii_in_the_historical_generation_through_the_real_kernel() => _result.ShouldEqual(ContentVerificationResult.Equal);

    void Destroy() => _scenario.Dispose();
}
