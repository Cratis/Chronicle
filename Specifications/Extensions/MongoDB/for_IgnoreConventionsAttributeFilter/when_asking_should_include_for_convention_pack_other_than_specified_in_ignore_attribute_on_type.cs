// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Conventions;

namespace Aksio.Cratis.Extensions.MongoDB.for_IgnoreConventionsAttributeFilter;

public class when_asking_should_include_for_convention_pack_other_than_specified_in_ignore_attribute_on_type : Specification
{
    [IgnoreConventions("TheOther")]
    record TheType();

    IgnoreConventionsAttributeFilter filter;

    bool result;

    void Establish() => filter = new();

    void Because() => result = filter.ShouldInclude("SomePack", Mock.Of<IConventionPack>(), typeof(TheType));

    [Fact] void should_include_it() => result.ShouldBeTrue();
}
