// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration.for_FixtureLifecycleHooks;

public class when_startup_and_cleanup_fail : Specification
{
    readonly IOException _startup = new("terminal start failure");
    readonly IOException _cleanup = new("unconfirmed stop");
    KernelFixtureCertificate _material;
    IContainer? _owned;
    Exception? _result;
    bool _materialPresentDuringDisposal;

    void Establish() => _material = KernelFixtureCertificate.Create();

    void Because()
    {
        var hooks = new FixtureLifecycleHooks
        {
            CreateMaterial = () => _material,
            ContainerBuilt = container => _owned = container,
            Start = (_, _) => Task.FromException(_startup),
            Dispose = (_, _) =>
            {
                _materialPresentDuringDisposal = File.Exists(_material.Path);
                return ValueTask.FromException(_cleanup);
            }
        };
        _result = Catch.Exception(() => _ = new FailingFixture(hooks));
    }

    async Task Destroy() => await _material.DisposeAfter(() => _owned?.DisposeAsync() ?? ValueTask.CompletedTask);

    [Fact] void should_preserve_the_original_start_failure() => _result.ShouldEqual(_startup);
    [Fact] void should_attach_the_cleanup_failure() => _result!.Data["FixtureCleanupFailure"].ShouldEqual(_cleanup);
    [Fact] void should_observe_ownership_after_constructor_failure() => _owned.ShouldNotBeNull();
    [Fact] void should_retain_material_until_stop_is_confirmed() => File.Exists(_material.Path).ShouldBeTrue();
    [Fact] void should_keep_material_during_disposal() => _materialPresentDuringDisposal.ShouldBeTrue();

    sealed class FailingFixture(FixtureLifecycleHooks hooks) : ChronicleFixture(hooks)
    {
        protected override IContainer BuildContainer(INetwork network)
        {
            KernelCertificate = LifecycleHooks!.CreateMaterial!();
            return Substitute.For<IContainer>();
        }
    }
}
