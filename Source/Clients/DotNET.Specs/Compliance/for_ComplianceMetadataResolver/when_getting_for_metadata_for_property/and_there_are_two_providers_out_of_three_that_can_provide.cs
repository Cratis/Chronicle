// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_property;

public class and_there_are_two_providers_out_of_three_that_can_provide : Specification
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadataResolver _resolver;

    ComplianceMetadata _firstProviderMetadata;
    ComplianceMetadata _thirdProviderMetadata;
    IEnumerable<ComplianceMetadata> result;
    ICanProvideComplianceMetadataForProperty _secondProvider;

    void Establish()
    {
        _firstProviderMetadata = new("134b380b-b298-4eda-bb81-5674ef326a32", "Details from first");
        var firstProvider = Substitute.For<ICanProvideComplianceMetadataForProperty>();
        firstProvider.CanProvide(MyClass.SomethingProperty).Returns(true);
        firstProvider.Provide(MyClass.SomethingProperty).Returns(_firstProviderMetadata);

        _secondProvider = Substitute.For<ICanProvideComplianceMetadataForProperty>();
        _secondProvider.CanProvide(MyClass.SomethingProperty).Returns(false);

        _thirdProviderMetadata = new("a933b65c-7166-43e1-8089-8d6c84d286aa", "Details from third");
        var thirdProvider = Substitute.For<ICanProvideComplianceMetadataForProperty>();
        thirdProvider.CanProvide(MyClass.SomethingProperty).Returns(true);
        thirdProvider.Provide(MyClass.SomethingProperty).Returns(_firstProviderMetadata);

        _resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([firstProvider, _secondProvider, thirdProvider])
        );
    }

    void Because() => result = _resolver.GetMetadataFor(MyClass.SomethingProperty);

    [Fact] void should_return_metadata_for_first_provider() => result.ToArray()[0].ShouldEqual(_firstProviderMetadata);
    [Fact] void should_not_ask_for_metadata_from_second_provider() => _secondProvider.DidNotReceive().Provide(Arg.Any<PropertyInfo>());
    [Fact] void should_return_metadata_for_third_provider() => result.ToArray()[1].ShouldEqual(_firstProviderMetadata);
}
