// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.for_ComplianceDetailsExtensions;

public class when_getting_details_from_type_with_attribute : Specification
{
    const string details = "This is the details";

    [ComplianceDetails(details)]
    class TheType { }

    string result;

    void Because() => result = typeof(TheType).GetComplianceMetadataDetails();

    [Fact] void should_return_the_details() => result.ShouldEqual(details);
}
