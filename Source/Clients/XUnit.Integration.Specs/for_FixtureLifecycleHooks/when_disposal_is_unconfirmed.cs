// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration.for_FixtureLifecycleHooks;

public class when_disposal_is_unconfirmed : Specification
{
    readonly IOException _failure = new("disposal not confirmed");
    FixtureLifecycleHooks _hooks;
    HookedFixture _fixture;
    KernelFixtureCertificate _material;
    Exception? _result;
    bool _materialPresentAtRecovery;

    void Establish()
    {
        _material = KernelFixtureCertificate.Create();
        _hooks = new()
        {
            Start = (_, _) => Task.CompletedTask,
            Dispose = (_, _) => ValueTask.FromException(_failure)
        };
        _fixture = new(_hooks) { KernelCertificate = _material };
    }

    async Task Because() => _result = await Catch.Exception(async () => await _fixture.DisposeAsync());

    async Task Destroy()
    {
        _hooks.Dispose = async (_, dispose) =>
        {
            _materialPresentAtRecovery = File.Exists(_material.Path);
            await dispose();
        };
        await _fixture.DisposeAsync();
        _materialPresentAtRecovery.ShouldBeTrue();
        File.Exists(_material.Path).ShouldBeFalse();
    }

    [Fact] void should_preserve_the_original_disposal_failure() => _result.ShouldEqual(_failure);
    [Fact] void should_keep_material_until_container_disposal_is_confirmed() => File.Exists(_material.Path).ShouldBeTrue();

    sealed class HookedFixture(FixtureLifecycleHooks hooks) : ChronicleFixture(hooks)
    {
        protected override IContainer BuildContainer(INetwork network) => Substitute.For<IContainer>();
    }
}
