// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_non_member_enums_differ_in_sql : given.a_storage_round_trip
{
    async Task Establish()
    {
        await Store(EnumSchema, NonMemberEnums, sql: true);
        _command = _command with { Content = """{"flags":0,"status":0}""" };
    }

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_not_substitute_the_declared_zero_member() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
