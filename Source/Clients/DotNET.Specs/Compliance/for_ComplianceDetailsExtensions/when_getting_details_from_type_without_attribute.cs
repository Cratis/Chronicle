// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceDetailsExtensions;

public class when_getting_details_from_type_without_attribute : Specification
{
    string _result;

    void Because() => _result = typeof(object).GetComplianceMetadataDetails();

    [Fact] void should_return_empty_string() => _result.ShouldEqual(string.Empty);
}
