// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

/// <summary>
/// The sequence keeps claims for as many imports as it may, and one of them stopped validating without releasing its
/// claims. Once they have gone untouched for longer than the idle timeout, they give way to an import that starts.
/// </summary>
public class and_an_import_left_its_claims_behind : given.an_event_sequence_with_imports
{
    readonly given.MovableClock _clock = new();
    readonly List<ImportId> _imports = [];
    ImportChunkValidationResult _newcomer;
    ImportChunkValidationResult _abandoned;
    ImportChunkValidationResult _active;

    protected override TimeProvider EventTimeProvider => _clock;

    async Task Because()
    {
        for (var index = 0; index < EventSequence.MaxImportsValidating; index++)
        {
            var importId = ImportId.New();
            _imports.Add(importId);
            await _eventSequence.ValidateImportChunk(importId, true, ImportEvents());
        }

        _clock.Now += EventSequence.ImportValidationIdleTimeout;
        foreach (var importId in _imports.Skip(1)) await _eventSequence.ValidateImportChunk(importId, false, ImportEvents());
        _clock.Now += TimeSpan.FromSeconds(1);

        _newcomer = await _eventSequence.ValidateImportChunk(ImportId.New(), true, ImportEvents());
        _abandoned = await _eventSequence.ValidateImportChunk(_imports[0], false, ImportEvents());
        _active = await _eventSequence.ValidateImportChunk(_imports[1], false, ImportEvents());
    }

    [Fact] void should_let_the_newcomer_validate() => (_newcomer.MustWait || _newcomer.MustRestart).ShouldBeFalse();
    [Fact] void should_have_released_the_claims_left_behind() => _abandoned.MustRestart.ShouldBeTrue();
    [Fact] void should_keep_the_claims_of_imports_still_validating() => _active.MustRestart.ShouldBeFalse();
}
