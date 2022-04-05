// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.for_ComplianceDetailsExtensions;

public class when_getting_details_from_property_with_details_attribute_on_declaring_type : Specification
{
    const string details = "This is the details";

    [ComplianceDetails(details)]
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    string result;

    void Because() => result = MyClass.SomethingProperty.GetComplianceMetadataDetails();

    [Fact] void should_return_the_details() => result.ShouldEqual(details);
}
