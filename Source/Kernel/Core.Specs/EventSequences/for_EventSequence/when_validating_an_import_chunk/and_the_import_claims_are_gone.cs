// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

public class and_the_import_claims_are_gone : given.an_event_sequence_with_imports
{
    async Task Because() => _validation = await _eventSequence.ValidateImportChunk(_importId, false, ImportEvents());

    [Fact] void should_ask_to_restart_from_the_first_chunk() => _validation.MustRestart.ShouldBeTrue();
}
