// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Projections;

public class SetValuesProjection : IProjectionFor<Model>
{
    public ProjectionId Identifier => "152b1348-e612-4165-bc49-fcaba94e8183";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EmptyEvent>(_ => _
            .Set(m => m.StringValue).ToValue(KnownValues.StringValue)
            .Set(m => m.BoolValue).ToValue(KnownValues.BoolValue)
            .Set(m => m.IntValue).ToValue(KnownValues.IntValue)
            .Set(m => m.FloatValue).ToValue(KnownValues.FloatValue)
            .Set(m => m.DoubleValue).ToValue(KnownValues.DoubleValue)
            .Set(m => m.EnumValue).ToValue(KnownValues.EnumValue)
            .Set(m => m.GuidValue).ToValue(KnownValues.GuidValue)
            .Set(m => m.DateTimeValue).ToValue(KnownValues.DateTimeValue)
            .Set(m => m.DateOnlyValue).ToValue(KnownValues.DateOnlyValue)
            .Set(m => m.TimeOnlyValue).ToValue(KnownValues.TimeOnlyValue)
            .Set(m => m.DateTimeOffsetValue).ToValue(KnownValues.DateTimeOffsetValue)
            .Set(m => m.StringConceptValue).ToValue(KnownValues.StringConceptValue)
            .Set(m => m.BoolConceptValue).ToValue(KnownValues.BoolConceptValue)
            .Set(m => m.IntConceptValue).ToValue(KnownValues.IntConceptValue)
            .Set(m => m.FloatConceptValue).ToValue(KnownValues.FloatConceptValue)
            .Set(m => m.DoubleConceptValue).ToValue(KnownValues.DoubleConceptValue)
            .Set(m => m.EnumConceptValue).ToValue(KnownValues.EnumConceptValue)
            .Set(m => m.GuidConceptValue).ToValue(KnownValues.GuidConceptValue)
            .Set(m => m.DateTimeConceptValue).ToValue(KnownValues.DateTimeConceptValue)
            .Set(m => m.DateOnlyConceptValue).ToValue(KnownValues.DateOnlyConceptValue)
            .Set(m => m.TimeOnlyConceptValue).ToValue(KnownValues.TimeOnlyConceptValue)
            .Set(m => m.DateTimeOffsetConceptValue).ToValue(KnownValues.DateTimeOffsetConceptValue));
}
