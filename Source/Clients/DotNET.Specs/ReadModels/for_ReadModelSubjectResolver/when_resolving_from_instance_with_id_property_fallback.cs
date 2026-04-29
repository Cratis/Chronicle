// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSubjectResolver;

public class when_resolving_from_instance_with_id_property_fallback : Specification
{
    class ReadModelWithIdProperty
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    Subject? _result;

    void Because() => _result = ReadModelSubjectResolver.ResolveFrom(new ReadModelWithIdProperty { Id = "id-99", Name = "Bob" });

    [Fact] void should_resolve_a_subject() => _result.ShouldNotBeNull();
    [Fact] void should_carry_the_id_value() => _result!.Value.ShouldEqual("id-99");
}
