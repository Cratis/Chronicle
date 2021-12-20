// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_property
{
    public class and_there_is : Specification
    {
        class MyClass
        {
            public string Something { get; set; }

            public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
        }

        ComplianceMetadataResolver resolver;

        ComplianceMetadata expected;
        ComplianceMetadata result;

        void Establish()
        {
            expected = new("134b380b-b298-4eda-bb81-5674ef326a32", "Some Details");
            var provider = new Mock<ICanProvideComplianceMetadataForProperty>();
            provider.Setup(_ => _.CanProvide(MyClass.SomethingProperty)).Returns(true);
            provider.Setup(_ => _.Provide(MyClass.SomethingProperty)).Returns(expected);

            resolver = new(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(Array.Empty<ICanProvideComplianceMetadataForType>()),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(new[] { provider.Object })
            );
        }

        void Because() => result = resolver.GetMetadataFor(MyClass.SomethingProperty);

        [Fact] void should_return_expected_metadata() => result.ShouldEqual(expected);
    }
}
