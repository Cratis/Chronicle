// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// A declaration that resolves to nothing must fail rather than fall back: falling back would put the value
/// straight back into one of the two silent outcomes the declaration exists to avoid.
/// </summary>
public class and_the_named_property_does_not_exist : Specification
{
    record DueSubject(string SubjectId, [PII][SubjectFrom("PersonId")] string Comment);

    Exception _result;

    void Because() => _result = Catch.Exception(() => ReadModelReleasePlan.For(typeof(DueSubject)));

    [Fact] void should_fail() => _result.ShouldBeOfExactType<ReleaseUnderPropertyNotFound>();
    [Fact] void should_name_the_read_model() => ((ReleaseUnderPropertyNotFound)_result).ReadModelType.ShouldEqual(typeof(DueSubject));
    [Fact] void should_name_the_declaring_property() => ((ReleaseUnderPropertyNotFound)_result).PropertyName.ShouldEqual(nameof(DueSubject.Comment));
    [Fact] void should_name_the_property_it_points_at() => ((ReleaseUnderPropertyNotFound)_result).SubjectPropertyName.ShouldEqual("PersonId");
    [Fact] void should_name_the_attribute_that_declares_it() => _result.Message.ShouldContain("[SubjectFrom(\"PersonId\")]");
}
