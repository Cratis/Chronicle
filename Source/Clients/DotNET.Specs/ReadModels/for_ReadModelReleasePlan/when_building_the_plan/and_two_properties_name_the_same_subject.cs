// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

public class and_two_properties_name_the_same_subject : Specification
{
    record DueSubject(
        string SubjectId,
        [PII][SubjectFrom(nameof(SubjectId))] string Comment,
        [PII][SubjectFrom(nameof(SubjectId))] string Reason);

    ReadModelReleasePlan _result;

    void Because() => _result = ReadModelReleasePlan.For(typeof(DueSubject));

    [Fact] void should_collapse_them_into_one_group() => _result.Groups.Count.ShouldEqual(1);
    [Fact] void should_hold_both_properties() => _result.Groups[0].Properties.Select(_ => _.Name).ShouldContainOnly([nameof(DueSubject.Comment), nameof(DueSubject.Reason)]);
}
