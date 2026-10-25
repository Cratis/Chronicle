// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OwnedFixture = Cratis.Chronicle.Integration.for_KernelTlsLifecycle.when_tls_is_refused.OwnedFixture;

namespace Cratis.Chronicle.Integration.for_KernelTlsLifecycle;

public class when_startup_fails : Cratis.Specifications.Specification
{
    [Fact] public async Task should_clean_owned_resources_when_the_pfx_is_missing()
    {
        Exception? nativeFailure = null;
        var logs = string.Empty;
        await using var owner = new OwnedFixture
        {
            MaterialFactory = () =>
            {
                var material = KernelFixtureCertificate.Create();
                File.Delete(material.Path);
                return material;
            },
            Start = async (container, start) =>
            {
                try
                {
                    await start();
                }
                catch (Exception failure)
                {
                    nativeFailure = failure;
                    var (stdout, stderr) = await container.GetLogsAsync();
                    logs = stdout + stderr;

                    // Terminal wrapper avoids an unrelated auxiliary-ownership retry redesign.
                    throw new IOException("Owned kernel could not load its missing PFX", failure);
                }
            }
        };
        var failure = Catch.Exception(owner.Construct);
        Assert.IsType<IOException>(failure);
        nativeFailure.ShouldNotBeNull();
        logs.ShouldContain("kernel.pfx");
        owner.KernelStopConfirmed.ShouldBeTrue();
        await owner.AssertGoneAsync();
        Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
    }

    [Fact] public async Task should_clean_owned_resources_after_a_started_attempt_fails()
    {
        var firstCause = new IOException("terminal failure after actual start");
        var started = false;
        await using var owner = new OwnedFixture
        {
            Start = async (_, start) =>
            {
                await start();
                started = true;
                throw firstCause;
            }
        };
        Catch.Exception(owner.Construct).ShouldEqual(firstCause);
        started.ShouldBeTrue();
        owner.KernelStopConfirmed.ShouldBeTrue();
        await owner.AssertGoneAsync();
        Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
    }

    [Fact] public async Task should_preserve_the_start_failure_when_rebuild_fails()
    {
        var firstCause = new TimeoutException("retryable failure after actual start");
        var rebuild = new IOException("terminal rebuild failure");
        var builds = 0;
        OwnedFixture? owner = null;
        owner = new()
        {
            Start = async (_, start) =>
            {
                await start();
                throw firstCause;
            },
            BeforeBuild = () =>
            {
                if (++builds != 2) return;
                owner!.KernelStopConfirmed.ShouldBeTrue();
                Directory.Exists(owner.MaterialDirectory).ShouldBeTrue();

                // Rebuild fails before creating another auxiliary container.
                throw rebuild;
            }
        };
        await using (owner)
        {
            Catch.Exception(owner.Construct).ShouldEqual(firstCause);
            builds.ShouldEqual(2);
            firstCause.Data["FixtureCleanupFailure"].ShouldEqual(rebuild);
            await owner.AssertGoneAsync();
            Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
        }
    }
}
