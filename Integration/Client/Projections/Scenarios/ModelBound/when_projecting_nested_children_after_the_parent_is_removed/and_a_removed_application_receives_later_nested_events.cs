// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed.given;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed.and_a_removed_application_receives_later_nested_events.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed;

[Collection(ChronicleCollection.Name)]
public class and_a_removed_application_receives_later_nested_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : a_project_tree(fixture)
    {
        public TreeProject Result;

        async Task Because()
        {
            await Subscribe();
            await Append(ProjectId, new TreeProjectRegistered("Project"));
            await Append(ApplicationId, new TreeApplicationCreatedForProject(ProjectId, "Application"));
            await Append(SiblingApplicationId, new TreeApplicationCreatedForProject(ProjectId, "Sibling"));
            await Append(DomainId, new TreeDomainCreatedForApplication(ApplicationId, "Domain"));
            await Append(SiblingDomainId, new TreeDomainCreatedForApplication(SiblingApplicationId, "Sibling domain"));
            await Append(ApplicationId, new TreeApplicationRemovedFromProject(ProjectId));

            // The project survives; only the application is gone when these arrive.
            await Append(DomainId, new TreeDomainRenamed(DomainId, ApplicationId, "Renamed domain"));
            await Append(EventModelId, new TreeEventModelCreatedForDomain(DomainId, "Event model"));
            await Append(DomainId, new TreeDomainRemoved(DomainId, ApplicationId));
            await ObserveThenReplay();

            Result = await EventStore.ReadModels.GetInstanceById<TreeProject>(ProjectId.ToString());
        }
    }

    [Fact] void should_not_fail_any_partition_while_observing() => Context.FailedPartitionsAfterObserving.ShouldBeEmpty();
    [Fact] void should_not_fail_any_partition_when_replaying() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_the_project() => Context.Result.Name.ShouldEqual("Project");
    [Fact] void should_keep_only_the_sibling_application() => Context.Result.Applications.Select(_ => _.Id).ShouldContainOnly([Context.SiblingApplicationId]);
    [Fact] void should_keep_the_sibling_domain() => Context.Result.Applications.Single().Domains.Select(_ => _.DomainId).ShouldContainOnly([Context.SiblingDomainId]);
    [Fact] void should_store_only_the_project_document() => Context.Documents.Count().ShouldEqual(Context.CanInspectDocuments ? 1 : 0);

    [Fact]
    void should_store_only_the_sibling_application()
    {
        if (!Context.CanInspectDocuments)
        {
            return;
        }

        Context.StoredApplicationIds.ShouldContainOnly((Guid?)Context.SiblingApplicationId);
    }
}
