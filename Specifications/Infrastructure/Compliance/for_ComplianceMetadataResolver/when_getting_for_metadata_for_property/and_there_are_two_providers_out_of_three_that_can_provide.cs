// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_property;

public class and_there_are_two_providers_out_of_three_that_can_provide : Specification
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadataResolver resolver;

    ComplianceMetadata first_provider_metadata;
    ComplianceMetadata third_provider_metadata;
    IEnumerable<ComplianceMetadata> result;
    Mock<ICanProvideComplianceMetadataForProperty> second_provider;

    void Establish()
    {
        first_provider_metadata = new("134b380b-b298-4eda-bb81-5674ef326a32", "Details from first");
        var firstProvider = new Mock<ICanProvideComplianceMetadataForProperty>();
        firstProvider.Setup(_ => _.CanProvide(MyClass.SomethingProperty)).Returns(true);
        firstProvider.Setup(_ => _.Provide(MyClass.SomethingProperty)).Returns(first_provider_metadata);

        second_provider = new Mock<ICanProvideComplianceMetadataForProperty>();
        second_provider.Setup(_ => _.CanProvide(MyClass.SomethingProperty)).Returns(false);

        third_provider_metadata = new("a933b65c-7166-43e1-8089-8d6c84d286aa", "Details from third");
        var thirdProvider = new Mock<ICanProvideComplianceMetadataForProperty>();
        thirdProvider.Setup(_ => _.CanProvide(MyClass.SomethingProperty)).Returns(true);
        thirdProvider.Setup(_ => _.Provide(MyClass.SomethingProperty)).Returns(first_provider_metadata);

        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>(Array.Empty<ICanProvideComplianceMetadataForType>()),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(new[] { firstProvider.Object, second_provider.Object, thirdProvider.Object })
        );
    }

    void Because() => result = resolver.GetMetadataFor(MyClass.SomethingProperty);

    [Fact] void should_return_metadata_for_first_provider() => result.ToArray()[0].ShouldEqual(first_provider_metadata);
    [Fact] void should_not_ask_for_metadata_from_second_provider() => second_provider.Verify(_ => _.Provide(IsAny<PropertyInfo>()), Never);
    [Fact] void should_return_metadata_for_third_provider() => result.ToArray()[1].ShouldEqual(first_provider_metadata);
}
