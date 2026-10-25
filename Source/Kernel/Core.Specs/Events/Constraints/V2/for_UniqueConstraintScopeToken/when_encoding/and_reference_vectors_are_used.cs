// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintScopeToken.when_encoding;

public class and_reference_vectors_are_used : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    readonly ResolvedConstraintScope[] _scopes = [new(), new("a|b", "c:d")];
    string[] _preimages;
    string[] _tokens;

    void Because()
    {
        _preimages = _scopes.Select(_ => Convert.ToHexString(UniqueConstraintScopeToken.GetPreimage(_)).ToLowerInvariant()).ToArray();
        _tokens = _scopes.Select(_ => UniqueConstraintScopeToken.Encode(_key, _).Value).ToArray();
    }

    // Preimages were written independently from the framing grammar; hashes computed with Node crypto.
    [Fact] void should_frame_all_unnarrowed_dimensions() => _preimages[0].ShouldEqual("6368726f6e69636c652f73636f70652f763200000000");
    [Fact] void should_tokenize_an_unscoped_constraint() => _tokens[0].ShouldEqual("45d8bbf4835ba1e84f773e330ba59079");
    [Fact] void should_frame_dimensions_in_typed_order() => _preimages[1].ShouldEqual("6368726f6e69636c652f73636f70652f7632000100000003617c620100000003633a6400");
    [Fact] void should_tokenize_delimiter_containing_dimensions() => _tokens[1].ShouldEqual("c15d1dea3222eadeaf2b60722b252bec");
}
