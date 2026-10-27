// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Large changes report complete counts but only the first twenty lines in each direction.
/// </summary>
public class when_the_difference_is_large : Specification
{
    Exception? _error;

    void Because()
    {
        var before = string.Join('\n', Enumerable.Range(0, 30).Select(index => $"removed {index.ToString("D2", CultureInfo.InvariantCulture)}")) + "\n";
        var after = string.Join('\n', Enumerable.Range(0, 30).Select(index => $"added {index.ToString("D2", CultureInfo.InvariantCulture)}")) + "\n";
        _error = Catch.Exception(() => given.public_api_difference.RequireMatch(before, after));
    }

    [Fact] void should_count_every_difference() => _error!.Message.ShouldContain("Removed: 30; added: 30.");
    [Fact] void should_include_the_first_removed_lines() => _error!.Message.ShouldContain("- removed 19");
    [Fact] void should_include_the_first_added_lines() => _error!.Message.ShouldContain("+ added 19");
    [Fact] void should_omit_later_removed_lines() => _error!.Message.ShouldNotContain("removed 20");
    [Fact] void should_omit_later_added_lines() => _error!.Message.ShouldNotContain("added 20");
}
