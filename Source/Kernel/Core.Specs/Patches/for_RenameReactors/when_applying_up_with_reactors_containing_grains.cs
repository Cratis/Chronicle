// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RenameReactors;

public class when_applying_up_with_reactors_containing_grains : given.a_rename_reactors_patch
{
    ReactorDefinition _reactorWithGrains;
    ReactorDefinition _reactorWithoutGrains;

    void Establish()
    {
        _reactorWithGrains = new ReactorDefinition(
            new ReactorId("MyNamespace.Grains.MyReactor"),
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            []);

        _reactorWithoutGrains = new ReactorDefinition(
            new ReactorId("MyNamespace.OtherReactor"),
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            []);

        _reactorStorage.GetAll().Returns(Task.FromResult<IEnumerable<ReactorDefinition>>(
            [_reactorWithGrains, _reactorWithoutGrains]));
    }

    async Task Because() => await _patch.Up();

    [Fact] void should_rename_reactor_with_grains() => _reactorStorage.Received(1).Rename(
        new ReactorId("MyNamespace.Grains.MyReactor"),
        new ReactorId("MyNamespace.MyReactor"));

    [Fact] void should_not_rename_reactor_without_grains() => _reactorStorage.DidNotReceive().Rename(
        new ReactorId("MyNamespace.OtherReactor"),
        Arg.Any<ReactorId>());
}
