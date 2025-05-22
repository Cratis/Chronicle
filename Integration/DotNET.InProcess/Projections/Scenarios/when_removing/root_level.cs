// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Models;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_removing.root_level.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_removing;

[Collection(ChronicleCollection.Name)]
public class root_level(context context) : Given<context>(context)
{
    public class context(ChronicleFixture ChronicleFixture) : given.a_projection_and_events_appended_to_it<ProjectionWithRootRemove, Model>(ChronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes), typeof(RemoveRoot)];

        void Establish()
        {
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
            EventsToAppend.Add(new RemoveRoot());
        }
    }

    [Fact] void should_not_have_any_result() => Context.Result.ShouldBeNull();
}
