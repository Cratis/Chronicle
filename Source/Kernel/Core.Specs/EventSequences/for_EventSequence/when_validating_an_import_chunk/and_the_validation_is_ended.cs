// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

public class and_the_validation_is_ended : given.an_event_sequence_with_imports
{
    async Task Because()
    {
        await _eventSequence.ValidateImportChunk(_importId, true, ImportEvents());
        await _eventSequence.EndImportValidation(_importId);
        _validation = await _eventSequence.ValidateImportChunk(_importId, false, ImportEvents());
    }

    [Fact] void should_have_released_the_claims() => _validation.MustRestart.ShouldBeTrue();
}
