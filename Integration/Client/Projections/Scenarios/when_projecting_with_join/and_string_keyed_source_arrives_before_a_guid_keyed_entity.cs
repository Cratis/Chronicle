// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join.and_string_keyed_source_arrives_before_a_guid_keyed_entity.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join;

[Collection(ChronicleCollection.Name)]
public class and_string_keyed_source_arrives_before_a_guid_keyed_entity(context context) : Given<context>(context)
{
    const string OrgNumber = "999888777";
    const string CompanyName = "Acme Corp";

    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ProjectionForGuidKeyedEngagementJoin, GuidKeyedEngagementSummary>(chronicleFixture)
    {
        public Guid EngagementId;
        public override IEnumerable<Type> EventTypes => [typeof(EngagementStarted), typeof(CompanyRegistered)];

        void Establish()
        {
            EngagementId = Guid.Parse("2f6b0c1d-4e50-4a61-9b72-1d3e4f506172");
            EventSourceId = EngagementId.ToString();
            ReadModelId = EngagementId.ToString();

            // Guards CHR-18: a string-keyed join source arriving BEFORE the Guid-keyed entity must not
            // freeze the partition. Its join value (a non-Guid org number) must never be materialized as a
            // root key for a Guid-keyed read model — the sink converts such a key gracefully instead of
            // throwing "Unrecognized Guid format" — and the entity must still materialize and pick up the
            // joined company name via the row-creation backfill (order-independent).
            EventsWithEventSourceIdToAppend.Add(new(OrgNumber, new CompanyRegistered(CompanyName)));
            EventsWithEventSourceIdToAppend.Add(new(EngagementId.ToString(), new EngagementStarted(OrgNumber)));
        }

        protected override Task<GuidKeyedEngagementSummary> GetReadModelResult() =>
            EventStore.ReadModels.GetInstanceById<GuidKeyedEngagementSummary>(EngagementId.ToString());
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_the_engagement_id() => Context.Result.Id.ShouldEqual(Context.EngagementId);
    [Fact] void should_keep_the_org_number() => Context.Result.CustomerOrgNumber.ShouldEqual(OrgNumber);
    [Fact] void should_join_the_company_name() => Context.Result.CustomerName.ShouldEqual(CompanyName);
}
