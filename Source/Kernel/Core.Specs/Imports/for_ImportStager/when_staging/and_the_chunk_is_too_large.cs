// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_the_chunk_is_too_large : given.a_stager
{
    async Task Because() => _result = await Stage(0, [.. Enumerable.Range(0, _options.Imports.MaxChunkEvents + 1).Select(_ => Event())]);

    [Fact] void should_refuse_the_chunk() => _result.Error.ShouldNotBeNull();
    [Fact] async Task should_stage_nothing() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
}
