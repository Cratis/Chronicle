// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_checking_retention;

public class and_the_import_has_outlived_it : given.an_import
{
    bool _abandoned;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        _time.Now = _time.Now.AddHours(25);
    }

    async Task Because() => _abandoned = await _grain.AbandonIfExpired();

    [Fact] void should_abandon_it() => _abandoned.ShouldBeTrue();
    [Fact] void should_be_abandoned() => Record.State.ShouldEqual(ImportState.Abandoned);
    [Fact] async Task should_discard_the_staging() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
}
