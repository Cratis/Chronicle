// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceDetailsExtensions;

public class when_getting_details_from_type_with_attribute : Specification
{
    const string Details = "This is the details";

    [ComplianceDetails(Details)]
    class TheType;

    string _result;

    void Because() => _result = typeof(TheType).GetComplianceMetadataDetails();

    [Fact] void should_return_the_details() => _result.ShouldEqual(Details);
}
