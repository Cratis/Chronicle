// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Projections;

public class ProjectionWithInitialValues : IProjectionFor<Model>
{
    public ProjectionId Identifier => "152b1348-e612-4165-bc49-fcaba94e8183";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .WithInitialValues(() => new(
            KnownValues.StringValue,
            KnownValues.BoolValue,
            KnownValues.IntValue,
            KnownValues.FloatValue,
            KnownValues.DoubleValue,
            KnownValues.EnumValue,
            KnownValues.GuidValue,
            KnownValues.DateTimeValue,
            KnownValues.DateOnlyValue,
            KnownValues.TimeOnlyValue,
            KnownValues.DateTimeOffsetValue,
            KnownValues.StringConceptValue,
            KnownValues.BoolConceptValue,
            KnownValues.IntConceptValue,
            KnownValues.FloatConceptValue,
            KnownValues.DoubleConceptValue,
            KnownValues.EnumConceptValue,
            KnownValues.GuidConceptValue,
            KnownValues.DateTimeConceptValue,
            KnownValues.DateOnlyConceptValue,
            KnownValues.TimeOnlyConceptValue,
            KnownValues.DateTimeOffsetConceptValue,
            DateTimeOffset.MinValue))
        .From<EmptyEvent>(_ => _.Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred));
}
