// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_schema_for_a_type_with_an_enum;

/// <summary>
/// The converter's withheld-default behavior reads nullability off the generated schema, and an enum is the one
/// case where the two do not obviously meet: a formatted scalar marks itself nullable with a trailing <c>?</c> on
/// its <c>format</c>, but an enum has no format, so the marker has to be the <c>"null"</c> entry in its type. If
/// the generator ever emitted a nullable enum some other way - a <c>oneOf</c>, a <c>$ref</c>, a bare
/// <c>"integer"</c> - the converter would read it as required and start writing type defaults again, while every
/// spec written against a hand-authored schema stayed green.
/// </summary>
/// <remarks>
/// The <c>"null"</c> entry itself is written by the <c>System.Text.Json</c> schema exporter, so no change to this
/// repository can make that emission stop - <see cref="should_mark_it_as_nullable"/> is a contract pin on an
/// upstream dependency, not a fence over Chronicle code. What it buys is a failing spec on the day that
/// dependency changes shape, which is exactly the day the fix silently stops working.
/// <para>
/// <see cref="should_withhold_a_default_for_it"/> is a statement of the behavior rather than a fence over it: a
/// 1-based nullable enum is withheld by nullability and by its member list at once, so removing either reading
/// leaves it passing. <see cref="should_withhold_a_default_for_a_declared_zero"/> is the one that can fail for a
/// single reason, and it is the same subject the converter's own specs use to separate the two.
/// </para>
/// </remarks>
public class and_it_is_nullable : given.a_contract_with_an_enum
{
    void Because() => _result = _generator.Generate(typeof(Contract));

    [Fact] void should_carry_the_declared_members() => MembersOf(nameof(Contract.Reason)).ShouldContainOnly(1L, 2L, 3L);
    [Fact] void should_mark_it_as_nullable() => PropertyNamed(nameof(Contract.Reason)).IsNullable().ShouldBeTrue();
    [Fact] void should_withhold_a_default_for_it() => PropertyNamed(nameof(Contract.Reason)).GetDefaultValue(_typeFormats).ShouldBeNull();
    [Fact] void should_mark_a_declared_zero_enum_as_nullable() => PropertyNamed(nameof(Contract.Feedback)).IsNullable().ShouldBeTrue();
    [Fact] void should_withhold_a_default_for_a_declared_zero() => PropertyNamed(nameof(Contract.Feedback)).GetDefaultValue(_typeFormats).ShouldBeNull();
}
