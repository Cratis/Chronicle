// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// The idiomatic placement — an attribute written without an explicit target on a positional record lands on
/// the primary constructor's parameter, not on the generated property. Reading only the property would make
/// the declaration do nothing exactly where it is most natural to write it.
/// </summary>
public class and_the_declaration_is_on_a_record_parameter : Specification
{
    record DueSubject(string SubjectId, [PII][ReleaseUnder(nameof(SubjectId))] string Comment);

    ReadModelReleasePlan _result;

    void Because() => _result = ReadModelReleasePlan.For(typeof(DueSubject));

    [Fact] void should_have_declarations() => _result.HasDeclarations.ShouldBeTrue();
    [Fact] void should_have_one_group() => _result.Groups.Count.ShouldEqual(1);
    [Fact] void should_release_under_the_named_property() => _result.Groups[0].SubjectProperty.Name.ShouldEqual(nameof(DueSubject.SubjectId));
    [Fact] void should_group_the_declared_property() => _result.Groups[0].Properties.Select(_ => _.Name).ShouldContainOnly([nameof(DueSubject.Comment)]);
}
