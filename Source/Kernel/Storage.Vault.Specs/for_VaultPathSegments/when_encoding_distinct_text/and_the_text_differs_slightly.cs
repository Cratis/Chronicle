// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.when_encoding_distinct_text;

public class and_the_text_differs_slightly : given.segments
{
    static readonly (string, string)[] _pairs =
    [
        ("a/b", "a~2Fb"),
        ("a/b", "a%2Fb"),
        ("a/b", "a\\b"),
        (".", "~2E"),
        ("a.", "a~2E"),
        ("~", "~7E"),
        ("\u00E9", "e\u0301"),
        ("A", "a")
    ];

    List<(string, string)> _sharing = [];
    string[] _encodedCorpus = [];
    List<string> _lost = [];

    void Because()
    {
        _sharing = [.. _pairs.Where(pair => Encode(pair.Item1) == Encode(pair.Item2))];
        _encodedCorpus = [.. HostileCorpus.Select(Encode)];
        _lost = [.. HostileCorpus.Where(value => Decode(Encode(value)) != value)];
    }

    [Fact] void should_give_each_pair_different_segments() => _sharing.ShouldBeEmpty();
    [Fact] void should_give_the_whole_corpus_different_segments() => _encodedCorpus.Distinct().Count().ShouldEqual(HostileCorpus.Length);
    [Fact] void should_lose_nothing() => _lost.ShouldBeEmpty();
}
