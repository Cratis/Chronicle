// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_Import.when_beginning;

public class and_it_exists_with_the_same_options : given.an_import
{
    async Task Establish() => await Begin();

    async Task Because() => _result = await _grain.Begin(Concepts.EventSequences.EventSequenceId.Log, Concepts.Imports.OnInvalidEvent.Reject);

    [Fact] void should_return_the_same_import() => _result.IsSuccess.ShouldBeTrue();
}
