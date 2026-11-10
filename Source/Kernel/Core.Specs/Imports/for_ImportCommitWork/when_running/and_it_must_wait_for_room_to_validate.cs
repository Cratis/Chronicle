// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

/// <summary>
/// The event sequence already validates as many imports as it keeps claims for, more times than validation may fail.
/// Waiting is not failing, so the import waits until there is room and then validates.
/// </summary>
public class and_it_must_wait_for_room_to_validate : given.an_import_being_committed
{
    const int Waits = 5;
    int _calls;

    void Establish() => _sequence.ValidateImportChunk(_key.ImportId, Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>()).Returns(call =>
    {
        _validationFirsts.Add(call.ArgAt<bool>(1));
        return ++_calls <= Waits ? ImportChunkValidationResult.Wait : new ImportChunkValidationResult([]);
    });

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_ask_again_as_a_first_chunk_until_there_is_room() => _validationFirsts.Take(Waits + 1).ShouldEachConformTo(_ => _);
    [Fact] void should_validate_every_chunk_once_there_is_room() => _validationFirsts.Count.ShouldEqual(Waits + 2);
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
