// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_property;

public class and_there_are_none : Specification
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadataResolver resolver;
    Exception result;

    void Establish() =>
        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([])
        );

    void Because() => result = Catch.Exception(() => resolver.GetMetadataFor(MyClass.SomethingProperty));

    [Fact] void should_throw_no_compliance_metadata_for_type() => result.ShouldBeOfExactType<NoComplianceMetadataForProperty>();
}
