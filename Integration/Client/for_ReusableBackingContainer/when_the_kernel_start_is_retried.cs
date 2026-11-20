// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ReusableBackingContainer;

public class when_the_kernel_start_is_retried : Cratis.Specifications.Specification
{
    IContainer _container;
    IContainer _first;
    IContainer _second;
    ReusableBackingContainer _owner;
    int _builds;
    int _starts;
    List<IContainer> _built;

    void Establish()
    {
        _container = Substitute.For<IContainer>();
        _built = [];
        _owner = new(
            () =>
            {
                _builds++;
                return _container;
            },
            _ =>
            {
                _starts++;
                return Task.CompletedTask;
            },
            _built.Add);
    }

    async Task Because()
    {
        _first = await _owner.GetOrStart();
        _second = await _owner.GetOrStart();
    }

    [Fact] void should_build_once() => _builds.ShouldEqual(1);
    [Fact] void should_start_once() => _starts.ShouldEqual(1);
    [Fact] void should_reuse_the_same_container() => _second.ShouldEqual(_first);
    [Fact] void should_observe_the_owned_container_once() => _built.Single().ShouldEqual(_container);
}
