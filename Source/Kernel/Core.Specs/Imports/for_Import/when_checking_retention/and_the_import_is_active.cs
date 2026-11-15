// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_checking_retention;

public class and_the_import_is_active : given.an_import
{
    bool _abandoned;

    async Task Establish()
    {
        await Begin();
        _time.Now = _time.Now.AddHours(23);
        await StageChunk(0, 3);
        _time.Now = _time.Now.AddHours(23);
    }

    async Task Because() => _abandoned = await _grain.AbandonIfExpired();

    [Fact] void should_count_retention_from_the_last_activity() => _abandoned.ShouldBeFalse();
    [Fact] void should_still_be_staging() => Record.State.ShouldEqual(ImportState.Staging);
}
