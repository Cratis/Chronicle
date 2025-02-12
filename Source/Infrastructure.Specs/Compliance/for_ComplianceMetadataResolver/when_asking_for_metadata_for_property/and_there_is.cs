// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_property;

public class and_there_is : Specification
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadataResolver _resolver;
    bool _result;

    void Establish()
    {
        var provider = Substitute.For<ICanProvideComplianceMetadataForProperty>();
        provider.CanProvide(MyClass.SomethingProperty).Returns(true);

        _resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([provider])
        );
    }

    void Because() => _result = _resolver.HasMetadataFor(MyClass.SomethingProperty);

    [Fact] void should_have() => _result.ShouldBeTrue();
}
