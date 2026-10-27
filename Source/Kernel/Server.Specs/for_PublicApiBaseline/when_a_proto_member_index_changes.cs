// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Wire indexes remain visible in the CLR baseline without replacing wire CI.
/// </summary>
public class when_a_proto_member_index_changes : given.a_contract_difference
{
    void Because() => Compare(_baseline, _baseline.Replace("ProtoMemberAttribute(System.Int32:3)", "ProtoMemberAttribute(System.Int32:4)", StringComparison.Ordinal));

    [Fact] void should_reject_the_change() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_old_index() => _error!.Message.ShouldContain("ProtoMemberAttribute(System.Int32:3)");
    [Fact] void should_name_the_new_index() => _error!.Message.ShouldContain("ProtoMemberAttribute(System.Int32:4)");
    [Fact] void should_identify_the_tagged_member() => _error!.Message.ShouldContain("::Names()");
}
