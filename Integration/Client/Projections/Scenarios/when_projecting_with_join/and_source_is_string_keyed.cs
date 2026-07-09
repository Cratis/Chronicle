// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join.and_source_is_string_keyed.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join;

[Collection(ChronicleCollection.Name)]
public class and_source_is_string_keyed(context context) : Given<context>(context)
{
    const string OrgNumber = "999888777";
    const string CompanyName = "Acme Corp";

    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ProjectionForEngagementJoin, EngagementSummary>(chronicleFixture)
    {
        public EventSourceId EngagementId;
        public override IEnumerable<Type> EventTypes => [typeof(EngagementStarted), typeof(CompanyRegistered)];

        void Establish()
        {
            EngagementId = Guid.Parse("7b9c1d2e-3f40-4a51-8b62-0c3d4e5f6a7b").ToString();
            EventSourceId = EngagementId;
            ReadModelId = EngagementId;

            EventsWithEventSourceIdToAppend.Add(new(EngagementId, new EngagementStarted(OrgNumber)));
            EventsWithEventSourceIdToAppend.Add(new(OrgNumber, new CompanyRegistered(CompanyName)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_the_org_number() => Context.Result.CustomerOrgNumber.ShouldEqual(OrgNumber);
    [Fact] void should_join_the_company_name() => Context.Result.CustomerName.ShouldEqual(CompanyName);
}
