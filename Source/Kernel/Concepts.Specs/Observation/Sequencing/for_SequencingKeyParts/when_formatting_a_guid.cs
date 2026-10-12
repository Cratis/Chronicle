// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyParts;

public class when_formatting_a_guid : Specification
{
    string _part;

    void Because() => _part = SequencingKeyParts.Canonical(Guid.Parse("ABCDEF01-2345-6789-ABCD-EF0123456789"));

    [Fact] void should_use_lowercase_dashed_text() => _part.ShouldEqual("abcdef01-2345-6789-abcd-ef0123456789");
}
