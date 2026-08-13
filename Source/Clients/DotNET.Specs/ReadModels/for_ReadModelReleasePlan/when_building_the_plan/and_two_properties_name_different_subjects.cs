// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// The multi-subject composed row, which is the shape a single-subject read model cannot express at all.
/// </summary>
public class and_two_properties_name_different_subjects : Specification
{
    record Pairing(
        string FirstPersonId,
        string SecondPersonId,
        [PII][SubjectFrom(nameof(FirstPersonId))] string FirstName,
        [PII][SubjectFrom(nameof(SecondPersonId))] string SecondName);

    ReadModelReleasePlan _result;

    void Because() => _result = ReadModelReleasePlan.For(typeof(Pairing));

    [Fact] void should_have_a_group_per_subject() => _result.Groups.Count.ShouldEqual(2);
    [Fact] void should_release_the_first_name_under_the_first_person() => GroupFor(nameof(Pairing.FirstPersonId)).Properties.Select(_ => _.Name).ShouldContainOnly([nameof(Pairing.FirstName)]);
    [Fact] void should_release_the_second_name_under_the_second_person() => GroupFor(nameof(Pairing.SecondPersonId)).Properties.Select(_ => _.Name).ShouldContainOnly([nameof(Pairing.SecondName)]);

    ReadModelReleaseGroup GroupFor(string subjectProperty) => _result.Groups.First(_ => _.SubjectProperty.Name == subjectProperty);
}
