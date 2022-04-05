// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_property;

public class and_there_is : Specification
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadataResolver resolver;
    bool result;

    void Establish()
    {
        var provider = new Mock<ICanProvideComplianceMetadataForProperty>();
        provider.Setup(_ => _.CanProvide(MyClass.SomethingProperty)).Returns(true);

        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>(Array.Empty<ICanProvideComplianceMetadataForType>()),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(new[] { provider.Object })
        );
    }

    void Because() => result = resolver.HasMetadataFor(MyClass.SomethingProperty);

    [Fact] void should_have() => result.ShouldBeTrue();
}
