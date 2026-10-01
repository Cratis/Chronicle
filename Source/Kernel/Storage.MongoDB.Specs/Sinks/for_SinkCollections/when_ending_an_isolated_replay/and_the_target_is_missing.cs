// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_ending_an_isolated_replay;

public class and_the_target_is_missing : given.an_isolated_replay
{
    Exception? _error;
    void Establish() => _names.Add("Model");
    async Task Because() => _error = await Catch.Exception(() => _collections.EndReplay(_context));
    [Fact] void should_leave_the_outcome_unproven() => _error.ShouldBeOfExactType<ReplayTargetMissing>();
    [Fact] void should_not_manufacture_a_replacement() => _database.DidNotReceive().CreateCollectionAsync(Arg.Any<string>(), Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_preserve_the_original() => _names.ShouldContainOnly("Model");
}
