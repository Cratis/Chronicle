// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Starting against an empty System event store would create a fresh administrator roster and fail with a message that
/// does not say the databases were renamed (#5690).
/// </summary>
public class and_the_system_data_sits_under_the_other_database_name : given.a_startup_task
{
    Exception _error;

    void Establish() => _storage.FindMisplacedSystemEventStoreData().Returns("holds events under '_system'");

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<SystemEventStoreDataMisplaced>();
    [Fact] void should_say_where_the_data_is() => _error.Message.ShouldContain("holds events under '_system'");
    [Fact] void should_log_the_refusal() => LogMessages.Any(_ => _.Contains("holds events under '_system'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] async Task should_not_have_applied_any_patch() => await _patchRunner.DidNotReceive().ApplyPatches(Arg.Any<CancellationToken>());
}
