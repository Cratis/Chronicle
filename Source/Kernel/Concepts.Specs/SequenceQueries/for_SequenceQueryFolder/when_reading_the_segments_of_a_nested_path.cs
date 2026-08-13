// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Concepts.for_SequenceQueryFolder;

public class when_reading_the_segments_of_a_nested_path : Specification
{
    SequenceQueryFolder _folder;
    string[] _result;

    void Establish() => _folder = "Diagnostics/Failures/Appends";

    void Because() => _result = [.. _folder.Segments];

    [Fact] void should_read_outermost_first() => _result.ShouldContainOnly("Diagnostics", "Failures", "Appends");
}
