// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_migrating_and_entries_are_left_for_review : given.a_migrator
{
    int? _conflicts;
    int? _ambiguous;

    async Task Because()
    {
        Returns(Report(copied: 1, conflicts: 1));
        _conflicts = await Migrate();
        Returns(Report(ambiguous: 1));
        _ambiguous = await Migrate();
    }

    [Fact] void should_ask_for_review_of_conflicts() => _conflicts.ShouldEqual(2);
    [Fact] void should_ask_for_review_of_ambiguous_entries() => _ambiguous.ShouldEqual(2);
}
