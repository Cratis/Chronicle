// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ContainerStartup.for_ReusableBackingContainer;

public class and_the_first_start_fails : Cratis.Specifications.Specification
{
    IContainer _failed;
    IContainer _replacement;
    IContainer _result;
    ReusableBackingContainer _owner;
    Exception _failure;
    InvalidOperationException _startFailure;
    int _builds;

    void Establish()
    {
        _failed = Substitute.For<IContainer>();
        _replacement = Substitute.For<IContainer>();
        _startFailure = new("Synthetic start failure");
        _owner = new(
            () => ++_builds == 1 ? _failed : _replacement,
            container => container == _failed ? Task.FromException(_startFailure) : Task.CompletedTask,
            _ => { });
    }

    async Task Because()
    {
        _failure = await Catch.Exception(_owner.GetOrStart);
        _result = await _owner.GetOrStart();
        await _owner.DisposeAsync();
    }

    [Fact] void should_preserve_the_start_failure() => _failure.ShouldEqual(_startFailure);
    [Fact] void should_build_a_fresh_container() => _builds.ShouldEqual(2);
    [Fact] void should_return_the_replacement() => _result.ShouldEqual(_replacement);
    [Fact] void should_dispose_the_failed_attempt_once() => _failed.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IContainer.DisposeAsync)).ShouldEqual(1);
    [Fact] void should_dispose_the_replacement_once() => _replacement.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IContainer.DisposeAsync)).ShouldEqual(1);
}
