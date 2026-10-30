// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.given;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting.and_joined_and_parent_state_has_been_cached.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

/// <summary>
/// Redaction after a completed replay does not resurrect cached joined or child content.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(ChronicleCollection.Name)]
public class and_joined_and_parent_state_has_been_cached(context context) : Given<context>(context)
{
    /// <summary>
    /// Warm projections followed by completed reference and parent-content mutations.
    /// </summary>
    /// <param name="fixture">The Chronicle fixture.</param>
    public class context(ChronicleFixture fixture) : joined_and_parent_state(fixture)
    {
        async Task Because()
        {
            await this.RedactEvent(ReferenceSequenceNumber, "remove the shared reference");
            await CaptureJoinedAmount(0);
            await this.RedactEvent(MutatedSequenceNumber, "remove the cached child content");
            await CaptureAfterMutation(30, 10, 10);
        }
    }

    [Fact] void should_clear_the_first_joined_value() => Context.JoinedDocuments.Single(model => model.Id == joined_and_parent_state.FirstJoinedDocument).Amount.ShouldEqual(0);
    [Fact] void should_clear_the_second_joined_value() => Context.JoinedDocuments.Single(model => model.Id == joined_and_parent_state.SecondJoinedDocument).Amount.ShouldEqual(0);
    [Fact] void should_preserve_the_unrelated_joined_value() => Context.JoinedDocuments.Single(model => model.Id == joined_and_parent_state.UnrelatedJoinedDocument).Amount.ShouldEqual(7);
    [Fact] void should_rebuild_from_both_handled_join_types() => Context.JoinedDocuments.Select(model => model.Id).Order().ShouldEqual([joined_and_parent_state.FirstJoinedDocument, joined_and_parent_state.SecondJoinedDocument, joined_and_parent_state.UnrelatedJoinedDocument]);
    [Fact] void should_not_resurrect_the_cached_child() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Items.Select(child => (child.Id, child.Amount)).OrderBy(child => child.Id).ShouldEqual([(content_mutation_projections.FirstSource, 10), (content_mutation_projections.SecondSource, 20)]);
    [Fact] void should_preserve_the_unrelated_parent() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Items.Select(child => (child.Id, child.Amount)).ShouldEqual([(content_mutation_projections.NeighborSource, 7)]);
}
