// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

public class SetPropertiesProjection : IProjectionFor<Model>
{
    public ProjectionId Identifier => "152b1348-e612-4165-bc49-fcaba94e8183";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<Event>(_ => _
            .Set(m => m.StringValue).To(e => e.StringValue)
            .Set(m => m.BoolValue).To(e => e.BoolValue)
            .Set(m => m.IntValue).To(e => e.IntValue)
            .Set(m => m.DoubleValue).To(e => e.DoubleValue)
            .Set(m => m.StringConceptValue).To(e => e.StringConceptValue)
            .Set(m => m.BoolConceptValue).To(e => e.BoolConceptValue)
            .Set(m => m.IntConceptValue).To(e => e.IntConceptValue)
            .Set(m => m.DoubleConceptValue).To(e => e.DoubleConceptValue));
}
