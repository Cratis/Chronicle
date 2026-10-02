// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_mongodb_replaces_non_member_enums_with_defaults : given.a_mongodb_round_trip
{
    async Task Establish() => await StoreInMongoDB(EnumSchema, NonMemberEnums);

    async Task Because() => _result = await Verify();

    [Fact] void should_not_claim_equality_for_unrepresentable_enums() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
