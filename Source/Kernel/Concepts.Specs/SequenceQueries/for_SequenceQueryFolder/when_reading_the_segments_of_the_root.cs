// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Concepts.for_SequenceQueryFolder;

/// <summary>
/// A query sitting directly under its scope has no folder at all, so the root must read as no
/// segments rather than as one empty-named folder the hierarchy would then try to render.
/// </summary>
public class when_reading_the_segments_of_the_root : Specification
{
    string[] _result;

    void Because() => _result = [.. SequenceQueryFolder.Root.Segments];

    [Fact] void should_have_no_segments() => _result.ShouldBeEmpty();
}
