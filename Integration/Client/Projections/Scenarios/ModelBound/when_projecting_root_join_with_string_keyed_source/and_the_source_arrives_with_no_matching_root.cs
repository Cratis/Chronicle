// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_root_join_with_string_keyed_source.and_the_source_arrives_with_no_matching_root.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_root_join_with_string_keyed_source;

[Collection(ChronicleCollection.Name)]
public class and_the_source_arrives_with_no_matching_root(context context) : Given<context>(context)
{
    const string OrgNumber = "123456785";
    const string CompanyName = "Acme Corp";

    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid EngagementId;
        public bool ObserverAdvanced;
        public RootJoinGuidSummary Result;

        public override IEnumerable<Type> EventTypes => [typeof(EngagementStarted), typeof(CompanyRegistered)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(RootJoinGuidSummary)];

        async Task Because()
        {
            EngagementId = Guid.Parse("2f6b0c1d-4e50-4a61-9b72-1d3e4f506172");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<RootJoinGuidSummary>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            // The join source lands on a non-Guid org-number stream with no engagement root referencing it.
            // If the string join value were coerced to the Guid read-model key (Guid.Parse) or materialized
            // as a root document, the observer would fail and this wait would time out (the CHR-18 freeze).
            var appendResult = await EventStore.EventLog.Append(OrgNumber, new CompanyRegistered(CompanyName));
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);
            ObserverAdvanced = true;

            // No engagement root exists for this org number, and the join value must not have been keyed as a
            // (Guid) root — so a keyed read for the engagement returns null rather than a poison/phantom root.
            Result = await EventStore.ReadModels.GetInstanceById<RootJoinGuidSummary>(EngagementId.ToString());
        }
    }

    [Fact] void should_advance_the_observer_without_freezing() => Context.ObserverAdvanced.ShouldBeTrue();
    [Fact] void should_not_materialize_a_root_for_the_orphan_join_value() => Context.Result.ShouldBeNull();
}
