// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Projections;

public class SetPropertiesProjection : IProjectionFor<Model>
{
    public ProjectionId Identifier => "152b1348-e612-4165-bc49-fcaba94e8183";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .Set(m => m.StringValue).To(e => e.StringValue)
            .Set(m => m.BoolValue).To(e => e.BoolValue)
            .Set(m => m.IntValue).To(e => e.IntValue)
            .Set(m => m.FloatValue).To(e => e.FloatValue)
            .Set(m => m.DoubleValue).To(e => e.DoubleValue)
            .Set(m => m.EnumValue).To(e => e.EnumValue)
            .Set(m => m.GuidValue).To(e => e.GuidValue)
            .Set(m => m.DateTimeValue).To(e => e.DateTimeValue)
            .Set(m => m.DateOnlyValue).To(e => e.DateOnlyValue)
            .Set(m => m.TimeOnlyValue).To(e => e.TimeOnlyValue)
            .Set(m => m.DateTimeOffsetValue).To(e => e.DateTimeOffsetValue)
            .Set(m => m.StringConceptValue).To(e => e.StringConceptValue)
            .Set(m => m.BoolConceptValue).To(e => e.BoolConceptValue)
            .Set(m => m.IntConceptValue).To(e => e.IntConceptValue)
            .Set(m => m.FloatConceptValue).To(e => e.FloatConceptValue)
            .Set(m => m.DoubleConceptValue).To(e => e.DoubleConceptValue)
            .Set(m => m.EnumConceptValue).To(e => e.EnumConceptValue)
            .Set(m => m.GuidConceptValue).To(e => e.GuidConceptValue)
            .Set(m => m.DateTimeConceptValue).To(e => e.DateTimeConceptValue)
            .Set(m => m.DateOnlyConceptValue).To(e => e.DateOnlyConceptValue)
            .Set(m => m.TimeOnlyConceptValue).To(e => e.TimeOnlyConceptValue)
            .Set(m => m.DateTimeOffsetConceptValue).To(e => e.DateTimeOffsetConceptValue));
}
