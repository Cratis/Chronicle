// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationIdentity;

public class when_creating_supported_identities : Specification
{
    string[] _displays;
    EventSequenceMutationIdentityCreationResult[] _results;

    void Establish() => _displays =
    [
        string.Empty,
        "event-log",
        "Event-Log",
        " leading and trailing ",
        "control-\u0001-\u001f",
        "\ufeffbom",
        "\u00e9",
        "e\u0301",
        "pair-\ud83d\ude00",
        new('a', 200),
        new('\u0800', 200),
        string.Concat(Enumerable.Repeat("\ud83d\ude00", 100))
    ];

    void Because() => _results = _displays.Select(EventSequenceMutationIdentity.TryCreate).ToArray();

    [Fact] void should_accept_every_supported_identity() => _results.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_preserve_every_display_exactly() => _results.Select(_ => _.Identity!.Display).ShouldContainOnly(_displays);
    [Fact] void should_encode_every_key_as_strict_utf8() => _results.Select(_ => _.Identity!.Key.Snapshot()).Select(Encoding.UTF8.GetString).ShouldContainOnly(_displays);
    [Fact] void should_initialize_the_explicit_empty_key() => _results[0].Identity!.Key.IsInitialized.ShouldBeTrue();
    [Fact] void should_keep_composed_and_decomposed_forms_distinct() => (_results[6].Identity!.Key != _results[7].Identity!.Key).ShouldBeTrue();
    [Fact] void should_keep_case_variants_distinct() => (_results[1].Identity!.Key != _results[2].Identity!.Key).ShouldBeTrue();
}
