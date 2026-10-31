// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_ArtifactIdNames.when_getting_the_name_segment;

public class of_an_explicit_id_without_dots : Specification
{
    string _result;

    void Because() => _result = ArtifactIdNames.NameSegmentOf("send-email");

    [Fact] void should_extract_the_name() => _result.ShouldEqual("send-email");
}
