// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBAuthorizationErrors.when_classifying;

public class and_it_is_not_a_mongo_exception : Specification
{
    bool _result;
    string? _command;

    void Because() => _result = MongoDBAuthorizationErrors.IsUnauthorized(new TimeoutException(), out _command);

    [Fact] void should_not_recognize_the_failure() => _result.ShouldBeFalse();
    [Fact] void should_not_return_a_command_name() => _command.ShouldBeNull();
}
