// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview;

public class and_a_root_join_follows_the_removal : given.a_projection_sequence_ending_after_removal
{
    IEnumerable<ExpandoObject> _result;

    void Establish() => ConfigureRootJoinAfterRemoval();

    async Task Because() => _result = await _grain.ProcessForPreview(
        EventStoreNamespaceName.Default,
        [CreatedEvent, RemovedEvent, RootJoinEvent],
        CreatePreviewReadModelDefinition());

    [Fact] void should_preserve_the_removal() => _result.ShouldBeEmpty();
}
