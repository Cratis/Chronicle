// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_beginning;

public class and_it_is_new : given.an_import
{
    async Task Because() => _result = await _grain.Begin(Concepts.EventSequences.EventSequenceId.Log, OnInvalidEvent.Reject);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_open() => Record.State.ShouldEqual(ImportState.Open);
    [Fact] async Task should_persist_it() => await _state.Received(1).WriteStateAsync();
}
