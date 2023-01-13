// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_property;

public class and_declaring_type_is_adorned_with_pii_attribute : given.a_provider
{
    [PII]
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    bool result;
    void Because() => result = provider.CanProvide(MyClass.SomethingProperty);

    [Fact] void should_be_able_to_provide() => result.ShouldBeTrue();
}
