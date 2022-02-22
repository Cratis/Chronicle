// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_property;

public class and_there_is_no_metadata : given.a_provider
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    Exception result;
    void Because() => result = Catch.Exception(() => provider.Provide(MyClass.SomethingProperty));

    [Fact] void should_throw_no_compliance_metadata_for_property() => result.ShouldBeOfExactType<NoComplianceMetadataForProperty>();
}
