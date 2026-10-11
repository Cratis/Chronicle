// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_ArtifactIdNames.when_getting_the_name_segment;

public class of_a_nested_type : Specification
{
    string _result;

    void Because() => _result = ArtifactIdNames.NameSegmentOf("App.Orders+SendEmail");

    [Fact] void should_extract_the_name() => _result.ShouldEqual("SendEmail");
}
