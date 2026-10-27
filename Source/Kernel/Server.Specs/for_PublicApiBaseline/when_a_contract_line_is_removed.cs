// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// A removed declaration cannot disappear from the baseline unnoticed.
/// </summary>
public class when_a_contract_line_is_removed : given.a_contract_difference
{
    string _removed = string.Empty;

    void Because()
    {
        _removed = _baseline.Split('\n').First(line => line.StartsWith("property ", StringComparison.Ordinal));
        Compare(_baseline, _baseline.Replace(_removed + "\n", string.Empty, StringComparison.Ordinal));
    }

    [Fact] void should_reject_the_change() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_removed_declaration() => _error!.Message.ShouldContain($"- {_removed}");
    [Fact] void should_include_the_change_counts() => _error!.Message.ShouldContain("Removed: 1; added: 0.");
}
