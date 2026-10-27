// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Additions require deliberate review as well as removals.
/// </summary>
public class when_a_contract_line_is_added : given.a_contract_difference
{
    const string Added = "property Sample.Response::Added() System.String! write=NotNull";

    void Because() => Compare(_baseline, string.Join('\n', _baseline.Split('\n', StringSplitOptions.RemoveEmptyEntries).Append(Added).Order(StringComparer.Ordinal)) + "\n");

    [Fact] void should_reject_the_change() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_added_declaration() => _error!.Message.ShouldContain($"+ {Added}");
    [Fact] void should_explain_deliberate_regeneration() => _error!.Message.ShouldContain("README.md before deliberate regeneration");
}
