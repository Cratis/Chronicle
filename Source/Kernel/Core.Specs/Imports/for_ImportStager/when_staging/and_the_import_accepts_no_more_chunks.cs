// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_the_import_accepts_no_more_chunks : given.a_stager
{
    void Establish() => _import.EnsureAcceptsChunks().Returns(ImportOperationResult.Refused(null, "completed"));

    async Task Because() => _result = await Stage(0, Event());

    [Fact] void should_refuse_the_chunk() => _result.Error.ShouldEqual("completed");
    [Fact] async Task should_stage_nothing() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
}
