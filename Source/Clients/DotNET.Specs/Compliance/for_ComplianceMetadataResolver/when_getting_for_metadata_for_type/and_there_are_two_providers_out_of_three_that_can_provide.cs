// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_type;

public class and_there_are_two_providers_out_of_three_that_can_provide : Specification
{
    ComplianceMetadataResolver _resolver;
    ComplianceMetadata _firstProviderMetadata;
    ComplianceMetadata _thirdProviderMetadata;
    IEnumerable<ComplianceMetadata> _result;
    ICanProvideComplianceMetadataForType _secondProvider;

    void Establish()
    {
        _firstProviderMetadata = new("134b380b-b298-4eda-bb81-5674ef326a32", "Details from first");
        var firstProvider = Substitute.For<ICanProvideComplianceMetadataForType>();
        firstProvider.CanProvide(typeof(object)).Returns(true);
        firstProvider.Provide(typeof(object)).Returns(_firstProviderMetadata);

        _secondProvider = Substitute.For<ICanProvideComplianceMetadataForType>();
        _secondProvider.CanProvide(typeof(object)).Returns(false);

        _thirdProviderMetadata = new("a933b65c-7166-43e1-8089-8d6c84d286aa", "Details from third");
        var thirdProvider = Substitute.For<ICanProvideComplianceMetadataForType>();
        thirdProvider.CanProvide(typeof(object)).Returns(true);
        thirdProvider.Provide(typeof(object)).Returns(_firstProviderMetadata);

        _resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([firstProvider, _secondProvider, thirdProvider]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([])
        );
    }

    void Because() => _result = _resolver.GetMetadataFor(typeof(object));

    [Fact] void should_return_metadata_for_first_provider() => _result.ToArray()[0].ShouldEqual(_firstProviderMetadata);
    [Fact] void should_not_ask_for_metadata_from_second_provider() => _secondProvider.DidNotReceive().Provide(Arg.Any<Type>());
    [Fact] void should_return_metadata_for_third_provider() => _result.ToArray()[1].ShouldEqual(_firstProviderMetadata);
}
