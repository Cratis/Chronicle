// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_running_startup_command.given;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_running_startup_command;

public class and_rs_initiate_fails : Specification
{
    int _exitCode;

    async Task Because() => (_exitCode, _) = await a_failed_initiation_command.Run();

    [Fact] void should_exit_with_the_mongosh_status() => _exitCode.ShouldEqual(23);
}
