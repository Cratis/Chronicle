// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture;

public class when_cleanup_fails_after_kernel_exit : Specification
{
    readonly KernelProcessExited _exit = new("kernel", 137, "Out of memory.");
    IChronicleFixture _chronicleFixture;
    Exception? _initialization;
    Exception? _cleanup;
    bool _scenarioCleanup;

    void Establish()
    {
        _chronicleFixture = Substitute.For<IChronicleFixture>();
        _chronicleFixture.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.FromException(new IOException("later reset timeout")));
    }

    async Task Because()
    {
        var fixture = new scenario_fixture(_chronicleFixture, _exit, () => _scenarioCleanup = true);
        _initialization = await Catch.Exception(fixture.InitializeAsync);
        _cleanup = await Catch.Exception(fixture.DisposeAsync);
    }

    [Fact] void should_fail_initialization_with_the_exit() => _initialization.ShouldEqual(_exit);
    [Fact] void should_keep_the_exit_as_the_primary_cleanup_cause() => _cleanup.ShouldEqual(_exit);
    [Fact] void should_keep_the_initial_diagnostics() => _cleanup!.Message.ShouldContain("137: Out of memory.");
    [Fact] void should_retain_the_original_throw_site() => _cleanup!.StackTrace!.ShouldContain("OnBeforeInitializeAsync");
    [Fact] void should_still_attempt_scenario_cleanup() => _scenarioCleanup.ShouldBeTrue();
    [Fact] Task should_still_attempt_storage_cleanup() => _chronicleFixture.Received(1).RemoveAllDatabases();

    sealed class scenario_fixture(IChronicleFixture fixture, KernelProcessExited exit, Action cleanup) : ChronicleClientFixture<IChronicleFixture>(fixture)
    {
        protected override IAsyncDisposable CreateWebApplicationFactory() => throw new InvalidOperationException("This harness must not build a host.");

        protected override void ConfigureWebHostBuilder(IWebHostBuilder builder) => throw new InvalidOperationException("This harness must not configure a host.");

        protected override Task OnBeforeInitializeAsync() => throw exit;

        protected override Task OnDisposeAsync()
        {
            cleanup();
            throw new IOException("later scenario cleanup failure");
        }
    }
}
