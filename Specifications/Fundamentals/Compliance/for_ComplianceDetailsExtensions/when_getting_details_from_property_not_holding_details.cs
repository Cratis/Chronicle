// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance.for_ComplianceDetailsExtensions;

public class when_getting_details_from_property_not_holding_details : Specification
{
    class MyClass
    {
        public string Something { get; set; }

        public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    string result;

    void Because() => result = MyClass.SomethingProperty.GetComplianceMetadataDetails();

    [Fact] void should_return_empty_string() => result.ShouldEqual(string.Empty);
}
