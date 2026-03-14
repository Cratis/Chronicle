// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;

public class SetPropertiesProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
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
            .Set(m => m.GuidConceptValue).To(e => e.GuidConceptValue));
}
