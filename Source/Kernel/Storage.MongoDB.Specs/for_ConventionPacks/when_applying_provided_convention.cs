// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Chronicle.Storage.MongoDB.for_ConventionPacks;

public class when_applying_provided_convention : Specification
{
    BsonMemberMap _chronicleMember = default!;
    BsonMemberMap _otherMember = default!;
    CamelCaseElementNameConvention _convention = default!;

    void Establish()
    {
        _chronicleMember = new BsonClassMap<ExampleDocument>().MapMember(document => document.DisplayName);
        _otherMember = new BsonClassMap<Version>().MapMember(version => version.Major);
        _convention = (CamelCaseElementNameConvention)((ConventionPack)new ConventionPacks().Provide().Single().ConventionPack).Single();
    }

    void Because()
    {
        _convention.Apply(_chronicleMember);
        _convention.Apply(_otherMember);
    }

    [Fact] void should_use_camel_case_for_chronicle_types() => _chronicleMember.ElementName.ShouldEqual("displayName");
    [Fact] void should_leave_other_namespaces_unchanged() => _otherMember.ElementName.ShouldEqual("Major");

    class ExampleDocument
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
