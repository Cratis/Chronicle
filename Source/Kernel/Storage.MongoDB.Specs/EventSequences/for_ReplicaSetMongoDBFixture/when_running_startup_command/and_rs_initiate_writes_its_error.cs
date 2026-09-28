// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_running_startup_command.given;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_running_startup_command;

public class and_rs_initiate_writes_its_error : Specification
{
    string _stderr;

    async Task Because() => (_, _stderr) = await a_failed_initiation_command.Run();

    [Fact] void should_retain_the_mongosh_output() => _stderr.ShouldContain("simulated initiation error");
}
