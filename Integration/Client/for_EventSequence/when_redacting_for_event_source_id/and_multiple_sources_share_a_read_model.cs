// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.given;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting_for_event_source_id.and_multiple_sources_share_a_read_model.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting_for_event_source_id;

/// <summary>
/// Redacting one source repairs several contributing observers without losing other sources.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(ChronicleCollection.Name)]
public class and_multiple_sources_share_a_read_model(context context) : Given<context>(context)
{
    /// <summary>
    /// Interleaved contributions with every event for the first source redacted in one mutation.
    /// </summary>
    /// <param name="fixture">The Chronicle fixture.</param>
    public class context(ChronicleFixture fixture) : content_mutation_projections(fixture)
    {
        async Task Because()
        {
            await this.RedactEventsForEventSource(FirstSource, "repair shared contributions", typeof(ContentContributionRecorded));
            await CaptureAfterMutation(20, 0, 0, sourceRemoved: true);
        }
    }

    [Fact] void should_rebuild_the_shared_total() => Context.CoupledDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Total.ShouldEqual(20);
    [Fact] void should_preserve_the_neighbor_total() => Context.CoupledDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Total.ShouldEqual(7);
    [Fact] void should_not_create_a_document_for_the_source() => Context.CoupledDocuments.Select(model => model.Id).Order().ShouldEqual([content_mutation_projections.NeighborDocument, content_mutation_projections.SharedDocument]);
    [Fact] void should_keep_only_the_other_source_in_the_shared_parent() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Items.Select(child => (child.Id, child.Amount)).ShouldEqual([(content_mutation_projections.SecondSource, 20)]);
    [Fact] void should_preserve_neighbor_children() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Items.Select(child => (child.Id, child.Amount)).ShouldEqual([(content_mutation_projections.NeighborSource, 7)]);
    [Fact] void should_not_create_a_parent_for_the_source() => Context.ChildrenDocuments.Select(model => model.Id).Order().ShouldEqual([content_mutation_projections.NeighborDocument, content_mutation_projections.SharedDocument]);
    [Fact] void should_remove_the_isolated_source_document() => Context.IsolatedDocuments.Select(model => model.Id).ShouldNotContain(content_mutation_projections.FirstSource);
    [Fact] void should_leave_the_other_isolated_source_unchanged() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.SecondSource).Total.ShouldEqual(20);
    [Fact] void should_leave_the_isolated_neighbor_unchanged() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.NeighborSource).Total.ShouldEqual(7);
    [Fact] void should_keep_exactly_the_remaining_isolated_documents() => Context.IsolatedDocuments.Select(model => model.Id).Order().ShouldEqual([content_mutation_projections.NeighborSource, content_mutation_projections.SecondSource]);
}
