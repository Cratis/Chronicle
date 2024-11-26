// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_type;

public class and_there_are_two_providers_out_of_three_that_can_provide : Specification
{
    ComplianceMetadataResolver resolver;
    ComplianceMetadata first_provider_metadata;
    ComplianceMetadata third_provider_metadata;
    IEnumerable<ComplianceMetadata> result;
    ICanProvideComplianceMetadataForType second_provider;

    void Establish()
    {
        first_provider_metadata = new("134b380b-b298-4eda-bb81-5674ef326a32", "Details from first");
        var firstProvider = Substitute.For<ICanProvideComplianceMetadataForType>();
        firstProvider.CanProvide(typeof(object)).Returns(true);
        firstProvider.Provide(typeof(object)).Returns(first_provider_metadata);

        second_provider = Substitute.For<ICanProvideComplianceMetadataForType>();
        second_provider.CanProvide(typeof(object)).Returns(false);

        third_provider_metadata = new("a933b65c-7166-43e1-8089-8d6c84d286aa", "Details from third");
        var thirdProvider = Substitute.For<ICanProvideComplianceMetadataForType>();
        thirdProvider.CanProvide(typeof(object)).Returns(true);
        thirdProvider.Provide(typeof(object)).Returns(first_provider_metadata);

        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([firstProvider, second_provider, thirdProvider]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([])
        );
    }

    void Because() => result = resolver.GetMetadataFor(typeof(object));

    [Fact] void should_return_metadata_for_first_provider() => result.ToArray()[0].ShouldEqual(first_provider_metadata);
    [Fact] void should_not_ask_for_metadata_from_second_provider() => second_provider.DidNotReceive().Provide(Arg.Any<Type>());
    [Fact] void should_return_metadata_for_third_provider() => result.ToArray()[1].ShouldEqual(first_provider_metadata);
}
