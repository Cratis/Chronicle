// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed.given;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed.and_the_application_then_the_project_are_removed.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed;

[Collection(ChronicleCollection.Name)]
public class and_the_application_then_the_project_are_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : a_project_tree(fixture)
    {
        async Task Because()
        {
            await Subscribe();
            await Append(ProjectId, new TreeProjectRegistered("Project"));
            await Append(ApplicationId, new TreeApplicationCreatedForProject(ProjectId, "Application"));
            await Append(DomainId, new TreeDomainCreatedForApplication(ApplicationId, "Domain"));
            await Append(EventModelId, new TreeEventModelCreatedForDomain(DomainId, "Event model"));
            await Append(ApplicationId, new TreeApplicationRemovedFromProject(ProjectId));
            await Append(ProjectId, new TreeProjectRemoved());

            // The domain is removed last, as cleanup that follows the removal of everything above it.
            await Append(DomainId, new TreeDomainRemoved(DomainId, ApplicationId));
            await ObserveThenReplay();
        }
    }

    [Fact] void should_not_fail_any_partition_while_observing() => Context.FailedPartitionsAfterObserving.ShouldBeEmpty();
    [Fact] void should_not_fail_any_partition_when_replaying() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_not_resurrect_the_project_while_observing() => a_project_tree.OnlyPlaceholdersRemain(Context.DocumentsAfterObserving).ShouldBeTrue();
    [Fact] void should_not_resurrect_the_project_when_replaying() => a_project_tree.OnlyPlaceholdersRemain(Context.Documents).ShouldBeTrue();
}
