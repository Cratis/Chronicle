// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// [ReleaseUnder] shipped before [SubjectFrom] renamed the idea, so a read model written against it is
/// released source that must keep planning to exactly the same thing. It derives from [SubjectFrom], which
/// is the whole mechanism: the plan looks for [SubjectFrom] once and finds both.
/// </summary>
public class and_the_declaration_uses_the_superseded_attribute : Specification
{
#pragma warning disable CS0618 // the superseded attribute is the subject of this specification
    record DueSubject(string Id, string PersonId, [PII][ReleaseUnder(nameof(PersonId))] string Comment);

    record NestedComment([PII][ReleaseUnder("PersonId")] string Text);

    class DueSubjectWithProperties
    {
        public string PersonId { get; set; } = string.Empty;

        [PII]
        [ReleaseUnder(nameof(PersonId))]
        public string Comment { get; set; } = string.Empty;
    }
#pragma warning restore CS0618

    record Nesting(string Id, string PersonId, NestedComment Comment);

    ReadModelReleasePlan _result;
    ReadModelReleasePlan _onProperty;
    Exception _nested;

    void Because()
    {
        _result = ReadModelReleasePlan.For(typeof(DueSubject));
        _onProperty = ReadModelReleasePlan.For(typeof(DueSubjectWithProperties));
        _nested = Catch.Exception(() => ReadModelReleasePlan.For(typeof(Nesting)));
    }

    [Fact] void should_have_one_group() => _result.Groups.Count.ShouldEqual(1);
    [Fact] void should_release_under_the_named_property() => _result.Groups[0].SubjectProperty.Name.ShouldEqual(nameof(DueSubject.PersonId));
    [Fact] void should_group_the_declared_property() => _result.Groups[0].Properties.Select(_ => _.Name).ShouldContainOnly([nameof(DueSubject.Comment)]);
    [Fact] void should_recognize_it_written_on_a_property() => _onProperty.Groups.Count.ShouldEqual(1);
    [Fact] void should_release_the_property_form_under_the_named_property() => _onProperty.Groups[0].SubjectProperty.Name.ShouldEqual(nameof(DueSubjectWithProperties.PersonId));
    [Fact] void should_group_the_property_form_declaration() => _onProperty.Groups[0].Properties.Select(_ => _.Name).ShouldContainOnly([nameof(DueSubjectWithProperties.Comment)]);
    [Fact] void should_refuse_it_below_the_read_model_too() => _nested.ShouldBeOfExactType<ReleaseUnderNotSupportedBelowReadModel>();
}
