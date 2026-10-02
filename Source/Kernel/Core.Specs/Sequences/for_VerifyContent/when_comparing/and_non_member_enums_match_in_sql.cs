// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_non_member_enums_match_in_sql : given.a_storage_round_trip
{
    async Task Establish() => await Store(EnumSchema, NonMemberEnums, sql: true);

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_compare_the_stored_enum_values() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
