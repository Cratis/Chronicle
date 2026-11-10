// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_beginning;

public class and_it_exists_with_other_options : given.an_import
{
    async Task Establish() => await Begin();

    async Task Because() => _result = await _grain.Begin(Concepts.EventSequences.EventSequenceId.Log, OnInvalidEvent.Skip);

    [Fact] void should_refuse() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_first_options() => Record.OnInvalid.ShouldEqual(OnInvalidEvent.Reject);
}
