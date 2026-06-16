// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_polymorphic_type : Specification
{
    public abstract class Shape
    {
        public int Sides { get; set; }
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public record Drawing(Shape Shape);

    JsonSchemaGenerator _generator;
    JsonSchema _result;

    void Establish()
    {
        var derivedTypes = Substitute.For<IDerivedTypes>();
        derivedTypes.HasDerivatives(typeof(Shape)).Returns(true);

        _generator = new(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            new DefaultNamingPolicy(),
            derivedTypes);
    }

    void Because() => _result = _generator.Generate(typeof(Drawing));

    [Fact] void should_emit_an_open_schema_for_the_polymorphic_property() =>
        _result.ActualProperties.Values.First().ActualTypeSchema.GetFlattenedProperties().ShouldBeEmpty();
}
