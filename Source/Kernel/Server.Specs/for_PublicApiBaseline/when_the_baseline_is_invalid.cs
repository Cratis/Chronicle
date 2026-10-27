// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// An absent or malformed baseline is never interpreted as accepting current declarations.
/// </summary>
public class when_the_baseline_is_invalid : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = new string?[] { null, string.Empty, "b\na\n", "a\na\n", "a\r\nb\r\n", "a\n\nb\n", "a\nb" }
        .Select(baseline => Catch.Exception(() => given.public_api_difference.RequireMatch(baseline, "a\nb\n"))).ToArray();

    [Fact] void should_reject_every_invalid_baseline() => _errors.All(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_include_addition_and_removal_counts_when_missing() => _errors[0]!.Message.ShouldContain("Removed: 0; added: 2.");
    [Fact] void should_not_dump_the_full_snapshot() => _errors[0]!.Message.ShouldNotContain("--- BEGIN CURRENT PUBLIC API ---");
}
