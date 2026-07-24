// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionStringLocality.when_checking_a_connection_string;

public class and_it_targets_a_remote_host : Specification
{
    const string ConnectionString = "mongodb://mongo.example.com:27017";
    bool _result;

    void Because() => _result = ConnectionStringLocality.IsNonLocal(ConnectionString);

    [Fact] void should_be_non_local() => _result.ShouldBeTrue();
}
