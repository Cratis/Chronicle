// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_property;

public class and_there_is_metadata_and_details : given.a_provider
{
    const string details = "These are the details";

    [PII]
    [ComplianceDetails(details)]
    class MyType { }

    class MyClass
    {
        public MyType Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    ComplianceMetadata result;

    void Because() => result = provider.Provide(MyClass.SomethingProperty);

    [Fact] void should_return_pii_metadata() => result.MetadataType.ShouldEqual(ComplianceMetadataType.PII);
    [Fact] void should_return_metadata_with_details() => result.Details.ShouldEqual(details);
}
