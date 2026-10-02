// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_mongodb_preserves_the_attempted_values : given.a_mongodb_round_trip
{
    async Task Establish() => await StoreInMongoDB(
        """
        {"type":"object","properties":{
          "value":{"type":"number","format":"decimal"},
          "status":{"type":"integer","enum":[0,1],"x-enumNames":["None","Complete"]},
          "offset":{"type":"string","format":"date-time-offset"}}}
        """,
        """{"value":0.25,"status":1,"offset":"2026-03-04T05:06:07+02:30"}""");

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_compare_the_backend_representation() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
