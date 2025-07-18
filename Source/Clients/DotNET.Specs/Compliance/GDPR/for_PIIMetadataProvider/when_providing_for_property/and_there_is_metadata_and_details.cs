// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_property;

public class and_there_is_metadata_and_details : given.a_provider
{
    const string Details = "These are the details";

    [PII]
    [ComplianceDetails(Details)]
    class MyType;

    class MyClass
    {
        public MyType Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(MyClass.SomethingProperty);

    [Fact] void should_return_pii_metadata() => _result.MetadataType.ShouldEqual(ComplianceMetadataType.PII);
    [Fact] void should_return_metadata_with_details() => _result.Details.ShouldEqual(Details);
}
