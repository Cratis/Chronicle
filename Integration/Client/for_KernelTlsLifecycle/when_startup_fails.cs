// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OwnedFixture = Cratis.Chronicle.Integration.for_KernelTlsLifecycle.when_tls_is_refused.OwnedFixture;

namespace Cratis.Chronicle.Integration.for_KernelTlsLifecycle;

public class when_startup_fails : Cratis.Specifications.Specification
{
    [Fact] public async Task should_clean_owned_resources_when_the_pfx_cannot_be_loaded()
    {
        Exception? nativeFailure = null;
        var logs = string.Empty;
        await using var owner = new OwnedFixture
        {
            MaterialFactory = () =>
            {
                // A missing file would make Docker refuse the bind mount before the kernel runs, and the kernel
                // itself falls back to a development certificate when the configured path does not exist.
                // Unreadable PKCS#12 content makes the kernel start and then fail to load its certificate.
                var material = KernelFixtureCertificate.Create();
                File.WriteAllBytes(material.Path, "not a pkcs12 certificate"u8.ToArray());
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
                    try
                    {
                        var (stdout, stderr) = await container.GetLogsAsync();
                        logs = stdout + stderr;
                    }
                    catch (Exception logFailure)
                    {
                        // Reading logs is evidence only; it must never replace the terminal startup failure.
                        logs = $"<container logs unavailable: {logFailure.GetType().FullName}>";
                    }

                    // The fixture retries when the thrown exception or its direct inner exception is a
                    // DockerApiException. Nesting the native failure one level deeper keeps this wrapper terminal.
                    throw new IOException("Owned kernel could not load its PFX", new AggregateException(failure));
                }
            }
        };
        var failure = Catch.Exception(owner.Construct);
        Assert.True(failure is IOException, $"Expected the terminal PFX startup failure; exception types: {when_tls_is_refused.ExceptionTypes(failure)}. Native exception types: {when_tls_is_refused.ExceptionTypes(nativeFailure)}. Wrapper preserved native: {ReferenceEquals(failure?.InnerException?.InnerException, nativeFailure)}. Kernel stopped: {owner.KernelStopConfirmed}");
        Assert.IsType<IOException>(failure);
        nativeFailure.ShouldNotBeNull();
        failure!.InnerException!.InnerException.ShouldEqual(nativeFailure);

        // CertificateLoader.LoadFromTls reads the configured file with X509CertificateLoader.LoadPkcs12FromFile, which
        // throws CryptographicException for content that is not PKCS#12 (Source/Kernel/Server/CertificateLoader.cs:22,44).
        logs.ShouldContain("System.Security.Cryptography.CryptographicException");
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
