// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_schema_for_a_type_with_an_enum;

/// <summary>
/// The other half of the same wire, and the half that keeps the first half honest: a nullability reading that
/// answered true for everything would satisfy the nullable case exactly as well as a correct one, and would take
/// the whole withheld-default decision with it - every required property would stop being written. The required
/// enum is what makes that indistinguishable pass distinguishable.
/// <para>
/// It is also the subject the member-list guard is left holding on its own. Nullability does not exempt it, so
/// what keeps <c>0</c> out of a 1-based enum here is only that <c>0</c> is not one of its declared members.
/// </para>
/// </summary>
public class and_it_is_not_nullable : given.a_contract_with_an_enum
{
    void Because() => _result = _generator.Generate(typeof(Contract));

    [Fact] void should_carry_the_declared_members() => MembersOf(nameof(Contract.Status)).ShouldContainOnly(1L, 2L, 3L);
    [Fact] void should_not_mark_it_as_nullable() => PropertyNamed(nameof(Contract.Status)).IsNullable().ShouldBeFalse();
    [Fact] void should_withhold_a_default_outside_its_declared_members() => PropertyNamed(nameof(Contract.Status)).GetDefaultValue(_typeFormats).ShouldBeNull();
}
