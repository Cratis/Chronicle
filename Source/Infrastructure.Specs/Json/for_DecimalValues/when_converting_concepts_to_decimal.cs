// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_concepts_to_decimal : Specification
{
    record DoubleConcept(double Value) : ConceptAs<double>(Value);

    record SingleConcept(float Value) : ConceptAs<float>(Value);

    [Fact] void should_round_a_double_concept_like_the_primitive() => decimal.GetBits((decimal)DecimalValues.ConvertTo(typeof(decimal), new DoubleConcept(0.23d))).ShouldEqual(decimal.GetBits(0.23m));
    [Fact] void should_round_a_float_concept_like_the_primitive() => decimal.GetBits((decimal)DecimalValues.ConvertTo(typeof(decimal), new SingleConcept(0.23f))).ShouldEqual(decimal.GetBits(0.23m));
    [Fact] void should_round_a_double_concept_to_nullable_decimal() => decimal.GetBits((decimal)DecimalValues.ConvertTo(typeof(decimal?), new DoubleConcept(0.23d))).ShouldEqual(decimal.GetBits(0.23m));
    [Fact] void should_keep_null_for_decimal_target() => DecimalValues.ConvertTo(typeof(decimal), null!).ShouldEqual(TypeConversion.Convert(typeof(decimal), null!));
    [Fact] void should_keep_null_for_nullable_decimal_target() => DecimalValues.ConvertTo(typeof(decimal?), null!).ShouldEqual(TypeConversion.Convert(typeof(decimal?), null!));
    [Fact] void should_round_a_primitive_double() => decimal.GetBits((decimal)DecimalValues.ConvertTo(typeof(decimal), 0.23d)).ShouldEqual(decimal.GetBits(0.23m));
}
