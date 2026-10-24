// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.given;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting.and_the_last_contributor_is_removed.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

/// <summary>
/// Removing a document's only contributor leaves no stale coupled or isolated document.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_last_contributor_is_removed(context context) : Given<context>(context)
{
    /// <summary>
    /// The neighbor's sole contribution is removed after the projection has already replayed.
    /// </summary>
    /// <param name="fixture">The Chronicle fixture.</param>
    public class context(ChronicleFixture fixture) : content_mutation_projections(fixture)
    {
        async Task Because()
        {
            await this.RedactEvent(Tail, "remove the only contributor");
            await CaptureWhen(() =>
                CoupledDocuments.All(model => model.Id != NeighborDocument) &&
                ChildrenDocuments.All(model => model.Id != NeighborDocument) &&
                IsolatedDocuments.All(model => model.Id != NeighborSource));
        }
    }

    [Fact] void should_remove_the_empty_document() => Context.CoupledDocuments.Select(model => model.Id).ShouldEqual(new[] { content_mutation_projections.SharedDocument });
    [Fact] void should_preserve_the_other_contributions() => Context.CoupledDocuments.Single().Total.ShouldEqual(33);
    [Fact] void should_remove_the_empty_parent() => Context.ChildrenDocuments.Select(model => model.Id).ShouldEqual(new[] { content_mutation_projections.SharedDocument });
    [Fact] void should_preserve_the_other_children() => Context.ChildrenDocuments.Single().Items.Select(child => (child.Id, child.Amount)).OrderBy(child => child.Id).ShouldEqual(new[] { (content_mutation_projections.FirstSource, 3), (content_mutation_projections.SecondSource, 20) });
    [Fact] void should_remove_the_empty_isolated_document() => Context.IsolatedDocuments.Select(model => model.Id).Order().ShouldEqual(new[] { content_mutation_projections.FirstSource, content_mutation_projections.SecondSource });
    [Fact] void should_preserve_the_first_isolated_total() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.FirstSource).Total.ShouldEqual(13);
    [Fact] void should_preserve_the_second_isolated_total() => Context.IsolatedDocuments.Single(model => model.Id == content_mutation_projections.SecondSource).Total.ShouldEqual(20);
}
