// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Exact canonical snapshots pass without modification.
/// </summary>
public class when_the_baseline_is_unchanged : given.a_contract_difference
{
    void Because() => Compare(_baseline, _baseline);

    [Fact] void should_accept_the_snapshot() => _error.ShouldBeNull();
}
