// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// A declaration below the read model names a property the type it sits on does not have, so it cannot be
/// honored. Reporting it is the point — quietly ignoring it is the silence the declaration exists to end.
/// </summary>
public class and_the_declaration_sits_inside_a_value_object : Specification
{
    record Postponement(string SubjectId, [PII][ReleaseUnder(nameof(SubjectId))] string Comment);

    record DueSubject(string SubjectId, Postponement Postponement);

    Exception _result;

    void Because() => _result = Catch.Exception(() => ReadModelReleasePlan.For(typeof(DueSubject)));

    [Fact] void should_fail() => _result.ShouldBeOfExactType<ReleaseUnderNotSupportedBelowReadModel>();
    [Fact] void should_name_the_read_model() => ((ReleaseUnderNotSupportedBelowReadModel)_result).ReadModelType.ShouldEqual(typeof(DueSubject));
    [Fact] void should_name_the_nested_type() => ((ReleaseUnderNotSupportedBelowReadModel)_result).DeclaringType.ShouldEqual(typeof(Postponement));
    [Fact] void should_name_the_nested_property() => ((ReleaseUnderNotSupportedBelowReadModel)_result).PropertyName.ShouldEqual(nameof(Postponement.Comment));
}
