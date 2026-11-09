// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RepeatMoveSystemEventStoreDatabases;

/// <summary>
/// A failing patch fails the host on every start, so a failed move is reported and left for an operator.
/// </summary>
public class when_the_move_fails : given.a_repeat_move_patch
{
    Exception _exception;

    void Establish() => _storage.MoveSystemEventStoreTo(Arg.Any<SystemEventStoreLocation>()).Returns(Task.FromException(new InvalidOperationException("not authorized")));

    async Task Because() => _exception = await Catch.Exception(() => _patch.Up());

    [Fact] void should_not_fail_the_patch() => _exception.ShouldBeNull();
}
