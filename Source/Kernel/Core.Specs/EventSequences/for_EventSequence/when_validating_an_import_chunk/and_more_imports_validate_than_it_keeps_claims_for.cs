// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

/// <summary>
/// As many imports as the sequence keeps claims for are in the middle of their validation when one more starts.
/// </summary>
public class and_more_imports_validate_than_it_keeps_claims_for : given.an_event_sequence_with_imports
{
    readonly List<ImportId> _imports = [];
    readonly List<ImportChunkValidationResult> _continued = [];
    ImportChunkValidationResult _newcomer;
    ImportChunkValidationResult _newcomerAfterOneFinished;

    async Task Because()
    {
        for (var index = 0; index < EventSequence.MaxImportsValidating; index++)
        {
            var importId = ImportId.New();
            _imports.Add(importId);
            await _eventSequence.ValidateImportChunk(importId, true, ImportEvents());
        }

        var newcomer = ImportId.New();
        _newcomer = await _eventSequence.ValidateImportChunk(newcomer, true, ImportEvents());
        foreach (var importId in _imports) _continued.Add(await _eventSequence.ValidateImportChunk(importId, false, ImportEvents()));

        await _eventSequence.EndImportValidation(_imports[0]);
        _newcomerAfterOneFinished = await _eventSequence.ValidateImportChunk(newcomer, true, ImportEvents());
    }

    [Fact] void should_tell_the_newcomer_to_wait() => _newcomer.MustWait.ShouldBeTrue();
    [Fact] void should_validate_nothing_for_the_newcomer_yet() => _newcomer.Rejections.ShouldBeEmpty();
    [Fact] void should_keep_the_claims_of_every_import_in_the_middle_of_its_validation() => _continued.ShouldEachConformTo(_ => !_.MustRestart && !_.MustWait);
    [Fact] void should_let_the_newcomer_validate_once_one_finishes() => (_newcomerAfterOneFinished.MustWait || _newcomerAfterOneFinished.MustRestart).ShouldBeFalse();
}
