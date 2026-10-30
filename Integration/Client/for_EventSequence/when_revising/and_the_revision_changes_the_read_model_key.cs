// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.given;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_revising.and_the_revision_changes_the_read_model_key.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

/// <summary>
/// A key-changing revision removes the old contribution and materializes the new document.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_revision_changes_the_read_model_key(context context) : Given<context>(context)
{
    const string MovedDocument = "moved-document-not-a-source";

    /// <summary>
    /// A contribution moves to another document after the projection has already replayed.
    /// </summary>
    /// <param name="fixture">The Chronicle fixture.</param>
    public class context(ChronicleFixture fixture) : content_mutation_projections(fixture)
    {
        async Task Because()
        {
            await this.ReviseEvent(MutatedSequenceNumber, new ContentContributionRecorded(MovedDocument, 5));
            await CaptureAfterMutation(30, 10, 15);
        }
    }

    [Fact] void should_remove_the_old_contribution() => Context.CoupledDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Total.ShouldEqual(30);
    [Fact] void should_create_the_moved_contribution() => Context.CoupledDocuments.Single(model => model.Id == MovedDocument).Total.ShouldEqual(5);
    [Fact] void should_preserve_the_neighbor() => Context.CoupledDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Total.ShouldEqual(7);
    [Fact] void should_have_exactly_three_documents() => Context.CoupledDocuments.Length.ShouldEqual(3);
    [Fact] void should_rebuild_the_original_parent() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Items.Select(child => (child.Id, child.Amount)).OrderBy(child => child.Id).ShouldEqual([(content_mutation_projections.FirstSource, 10), (content_mutation_projections.SecondSource, 20)]);
    [Fact] void should_create_the_new_parent() => Context.ChildrenDocuments.Single(model => model.Id == MovedDocument).Items.Select(child => (child.Id, child.Amount)).ShouldEqual([(content_mutation_projections.FirstSource, 5)]);
    [Fact] void should_keep_the_isolated_total_independent_of_document_keys() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.FirstSource).Total.ShouldEqual(15);
}
