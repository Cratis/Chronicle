// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_model_was_removed : given.a_decision_reader
{
    DecisionRead<Model> _read;

    void Establish() => _json = "null";

    async Task Because() => _read = await _reader.GetDetached<Model>("source");

    [Fact] void should_read_as_absent() => _read.Exists.ShouldBeFalse();
}
