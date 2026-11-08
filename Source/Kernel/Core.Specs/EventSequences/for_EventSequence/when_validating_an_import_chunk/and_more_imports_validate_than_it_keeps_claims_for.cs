// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

public class and_more_imports_validate_than_it_keeps_claims_for : given.an_event_sequence_with_imports
{
    readonly List<ImportId> _imports = [];
    ImportChunkValidationResult _longestWaiting;
    ImportChunkValidationResult _mostRecent;

    async Task Because()
    {
        for (var index = 0; index <= EventSequence.MaxImportsValidating; index++)
        {
            var importId = ImportId.New();
            _imports.Add(importId);
            await _eventSequence.ValidateImportChunk(importId, true, ImportEvents());
        }

        _mostRecent = await _eventSequence.ValidateImportChunk(_imports[^1], false, ImportEvents());
        _longestWaiting = await _eventSequence.ValidateImportChunk(_imports[0], false, ImportEvents());
    }

    [Fact] void should_drop_the_claims_of_the_import_that_waited_longest() => _longestWaiting.MustRestart.ShouldBeTrue();
    [Fact] void should_keep_the_claims_of_the_others() => _mostRecent.MustRestart.ShouldBeFalse();
}
