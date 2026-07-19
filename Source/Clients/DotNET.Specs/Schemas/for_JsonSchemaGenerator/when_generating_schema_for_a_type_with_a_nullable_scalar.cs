// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// A nullable known-type scalar (e.g. <c>DateTimeOffset?</c>) must carry the nullable marker in its schema
/// format so <see cref="JsonSchemaExtensions.IsNullable"/> returns true and <see cref="JsonSchemaExtensions.GetDefaultValue"/>
/// yields <see langword="null"/> rather than the type-default sentinel (<c>0001-01-01</c>). Without the marker
/// an unset optional read-model value materializes at rest as that sentinel instead of null/absent.
/// </summary>
public class when_generating_schema_for_a_type_with_a_nullable_scalar : given.a_json_schema_generator
{
    record OrderTimeline(Guid Id, DateTimeOffset PlacedAt, DateTimeOffset? CompletedAt);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(OrderTimeline));

    JsonSchemaProperty PropertyNamed(string name) => _result.GetFlattenedProperties().Single(_ => _.Name == name);

    [Fact] void should_mark_the_optional_scalar_as_nullable() => PropertyNamed(nameof(OrderTimeline.CompletedAt)).IsNullable().ShouldBeTrue();
    [Fact] void should_default_the_optional_scalar_to_null() => PropertyNamed(nameof(OrderTimeline.CompletedAt)).GetDefaultValue(_typeFormats).ShouldBeNull();
    [Fact] void should_not_mark_the_required_scalar_as_nullable() => PropertyNamed(nameof(OrderTimeline.PlacedAt)).IsNullable().ShouldBeFalse();
    [Fact] void should_default_the_required_scalar_to_its_type_default() => PropertyNamed(nameof(OrderTimeline.PlacedAt)).GetDefaultValue(_typeFormats).ShouldNotBeNull();
}
