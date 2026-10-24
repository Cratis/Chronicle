// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.given;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_revising.and_the_read_model_key_differs_from_the_event_source.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

/// <summary>
/// Revision rebuilds shared documents and resets only the affected isolated source.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_read_model_key_differs_from_the_event_source(context context) : Given<context>(context)
{
    /// <summary>
    /// Interleaved contributions with the last contribution of the first source revised.
    /// </summary>
    /// <param name="fixture">The Chronicle fixture.</param>
    public class context(ChronicleFixture fixture) : content_mutation_projections(fixture)
    {
        async Task Because()
        {
            await this.ReviseEvent(MutatedSequenceNumber, new ContentContributionRecorded(SharedDocument, 5));
            await CaptureAfterMutation(35, 5, 15);
        }
    }

    [Fact] void should_rebuild_the_shared_total() => Context.CoupledDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Total.ShouldEqual(35);
    [Fact] void should_preserve_the_neighbor_total() => Context.CoupledDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Total.ShouldEqual(7);
    [Fact] void should_not_create_a_document_for_the_source() => Context.CoupledDocuments.Select(model => model.Id).Order().ShouldEqual(new[] { content_mutation_projections.NeighborDocument, content_mutation_projections.SharedDocument });
    [Fact] void should_rebuild_children_without_duplicates() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Items.Select(child => (child.Id, child.Amount)).OrderBy(child => child.Id).ShouldEqual(new[] { (content_mutation_projections.FirstSource, 5), (content_mutation_projections.SecondSource, 20) });
    [Fact] void should_preserve_neighbor_children() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Items.Select(child => (child.Id, child.Amount)).ShouldEqual(new[] { (content_mutation_projections.NeighborSource, 7) });
    [Fact] void should_not_create_a_parent_for_the_source() => Context.ChildrenDocuments.Select(model => model.Id).Order().ShouldEqual(new[] { content_mutation_projections.NeighborDocument, content_mutation_projections.SharedDocument });
    [Fact] void should_reset_the_isolated_source_before_refolding() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.FirstSource).Total.ShouldEqual(15);
    [Fact] void should_leave_the_other_isolated_source_unchanged() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.SecondSource).Total.ShouldEqual(20);
    [Fact] void should_leave_the_isolated_neighbor_unchanged() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.NeighborSource).Total.ShouldEqual(7);
    [Fact] void should_keep_exactly_the_isolated_source_documents() => Context.IsolatedDocuments.Select(model => model.Id).Order().ShouldEqual(new[] { content_mutation_projections.FirstSource, content_mutation_projections.NeighborSource, content_mutation_projections.SecondSource });
}
