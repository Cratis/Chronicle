// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Parameter defaults are CLR API even when the protobuf shape is unchanged.
/// </summary>
public class when_a_contract_signature_changes : given.a_contract_difference
{
    void Because()
    {
        var baseline = given.public_api_snapshot.Render([typeof(given.a_sample_message)]);
        Compare(baseline, baseline.Replace("optional=7", "optional=8", StringComparison.Ordinal));
    }

    [Fact] void should_reject_the_change() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_old_signature() => _error!.Message.ShouldContain("optional=7");
    [Fact] void should_name_the_new_signature() => _error!.Message.ShouldContain("optional=8");
    [Fact] void should_report_removed_and_added_lines() => _error!.Message.ShouldContain("+ method ");
}
