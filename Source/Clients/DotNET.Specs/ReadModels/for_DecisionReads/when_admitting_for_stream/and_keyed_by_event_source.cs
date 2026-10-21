// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_admitting_for_stream;

public class and_keyed_by_event_source : given.a_decision_reader
{
    DecisionReadAdmission _result;

    void Because() => _result = _reader.AdmitForStream<Model>();

    [Fact] void should_admit_the_projection() => _result.IsAdmitted.ShouldBeTrue();
}
