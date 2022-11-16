// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

public class SetValuesProjection : IProjectionFor<Model>
{
    public static string StringValue = "Forty two";
    public static bool BoolValue = true;
    public static int IntValue = 42;
    public static double DoubleValue = 42.42;
    public static StringConcept StringConceptValue = "Forty three";
    public static BoolConcept BoolConceptValue = true;
    public static IntConcept IntConceptValue = 43;
    public static DoubleConcept DoubleConceptValue = 43.43;

    public ProjectionId Identifier => "152b1348-e612-4165-bc49-fcaba94e8183";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EmptyEvent>(_ => _
            .Set(m => m.StringValue).ToValue(StringValue)
            .Set(m => m.BoolValue).ToValue(BoolValue)
            .Set(m => m.IntValue).ToValue(IntValue)
            .Set(m => m.DoubleValue).ToValue(DoubleValue)
            .Set(m => m.StringConceptValue).ToValue(StringConceptValue)
            .Set(m => m.BoolConceptValue).ToValue(BoolConceptValue)
            .Set(m => m.IntConceptValue).ToValue(IntConceptValue)
            .Set(m => m.DoubleConceptValue).ToValue(DoubleConceptValue));
}
