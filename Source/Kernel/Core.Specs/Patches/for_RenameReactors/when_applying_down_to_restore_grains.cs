// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RenameReactors;

public class when_applying_down_to_restore_grains : given.a_rename_reactors_patch
{
    ReactorDefinition _reactorWithoutGrains;

    void Establish()
    {
        _reactorWithoutGrains = new ReactorDefinition(
            new ReactorId("MyNamespace.MyReactor"),
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            []);

        _reactorStorage.GetAll().Returns(Task.FromResult<IEnumerable<ReactorDefinition>>(
            [_reactorWithoutGrains]));
    }

    async Task Because() => await _patch.Down();

    [Fact] void should_restore_grains_in_name() => _reactorStorage.Received(1).Rename(
        new ReactorId("MyNamespace.MyReactor"),
        new ReactorId("MyNamespace.Grains.MyReactor"));
}
