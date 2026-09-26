// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Chronicle.Storage.MongoDB.for_ConventionPacks;

public class when_providing_conventions : Specification
{
    ConventionPack _pack = default!;
    string _name = string.Empty;

    void Because()
    {
        var definition = new ConventionPacks().Provide().Single();
        _name = definition.Name;
        _pack = (ConventionPack)definition.ConventionPack;
    }

    [Fact] void should_return_the_camel_case_pack_name() => _name.ShouldEqual("CamelCase");
    [Fact] void should_return_the_camel_case_convention() => _pack.Single().ShouldBeOfExactType<CamelCaseElementNameConvention>();
    [Fact] void should_not_register_the_provided_convention() => ((ConventionPack)ConventionRegistry.Lookup(typeof(ConventionPacks))).ShouldNotContain(_pack.Single());
}
