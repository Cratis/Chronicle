// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_abandoning;

public class and_its_commit_has_started : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
    }

    async Task Because() => _result = await _grain.Abandon("too late");

    [Fact] void should_refuse() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_committing() => Record.State.ShouldEqual(ImportState.Validating);
}
