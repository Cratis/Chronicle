// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Authentication;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Grpc.Core;

namespace Cratis.Chronicle.Integration.for_KernelTlsLifecycle;

public class when_tls_is_refused : Cratis.Specifications.Specification
{
    [Fact] public Task should_refuse_a_wrong_pin() => RefuseReset(null, _ => new string('0', 64), control: true);

    [Fact] public async Task should_refuse_a_different_server_certificate()
    {
        var other = KernelFixtureCertificate.Create();
        try
        {
            await RefuseReset(() => KernelFixtureCertificate.Create(), _ => other.Hash, control: true);
        }
        finally
        {
            await other.DisposeAfter(() => ValueTask.CompletedTask);
        }
    }

    [Fact] public Task should_refuse_a_matching_pin_with_the_wrong_hostname() =>
        RefuseReset(() => KernelFixtureCertificate.CreateForHost("not-localhost.invalid"), hash => hash, control: false);

    static async Task RefuseReset(Func<KernelFixtureCertificate>? material, Func<string, string> pin, bool control)
    {
        await using var owner = new OwnedFixture { MaterialFactory = material };
        owner.Construct();

        // Positive control proves the selected image exposes the real development RPC.
        if (control) await owner.Fixture!.ResetForLifecycleProofAsync().WaitAsync(TimeSpan.FromSeconds(30));
        owner.BeginObservation();
        owner.Pin = pin;
        var reset = owner.Fixture!.ResetForLifecycleProofAsync();
        try
        {
            var native = await owner.Issued.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var refusal = await Catch.Exception(async () => await native.WaitAsync(TimeSpan.FromSeconds(15)));
            var rpc = Assert.IsType<RpcException>(refusal);
            rpc.StatusCode.ShouldEqual(StatusCode.Unavailable);
            var cause = rpc.Status.DebugException;
            while (cause is not (null or AuthenticationException)) cause = cause.InnerException;
            cause.ShouldNotBeNull();
        }
        finally
        {
            await owner.KillAsync();
            var terminal = await Catch.Exception(async () => await reset.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.IsNotType<TimeoutException>(terminal);
            reset.IsCompleted.ShouldBeTrue();
        }
        await owner.DisposeFixtureAsync();
        await owner.AssertGoneAsync();
        Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
    }

    /// <summary>
    /// Observes scenario-owned resources; emergency cleanup never satisfies fixture cleanup assertions.
    /// </summary>
    internal sealed class OwnedFixture : IAsyncDisposable
    {
        readonly List<IContainer> _containers = [];
        readonly HashSet<string> _createdIds = [];
        bool _fixtureDisposed;

        internal Func<KernelFixtureCertificate>? MaterialFactory { get; init; }
        internal Func<string, string>? Pin { get; set; }
        internal Func<IContainer, Func<Task>, Task>? Start { get; init; }
        internal Action? BeforeBuild { get; init; }
        internal Func<IContainer, Func<ValueTask>, ValueTask>? Dispose { get; set; }
        internal TaskCompletionSource<Task> Issued { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int IssuedCount { get; private set; }
        internal KernelFixtureCertificate? Material { get; private set; }
        internal string MaterialDirectory => Path.GetDirectoryName(Material!.Path)!;
        internal ChronicleConfigurableFixture? Fixture { get; private set; }
        internal IContainer Kernel => _containers[^1];
        internal bool KernelStopConfirmed { get; private set; }

        internal void Construct()
        {
            var options = ChronicleRuntimeOptions.Parse();
            options.Mode.ShouldEqual(ChronicleRuntimeMode.OutOfProcess);
            options.StorageProvider.ShouldEqual(ChronicleStorageProvider.MongoDB);
            Fixture = new(new FixtureLifecycleHooks
            {
                CreateMaterial = () => Material = MaterialFactory?.Invoke() ?? KernelFixtureCertificate.Create(),
                BeforeBuild = BeforeBuild,
                ContainerBuilt = _containers.Add,
                Start = (container, start) => Start?.Invoke(container, start) ?? start(),
                Dispose = DisposeObserved,
                ResetPin = hash => Pin?.Invoke(hash) ?? hash,
                ResetIssued = native =>
                {
                    IssuedCount++;
                    Issued.TrySetResult(native);
                }
            });
        }

        internal void BeginObservation() => Issued = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal async Task AssertGoneAsync()
        {
            var ids = _createdIds.Concat(_containers.Select(container => container.Id)).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToArray();
            ids.Length.ShouldBeGreaterThan(0);
            foreach (var id in ids) await AssertGone(id);
        }

        internal async Task KillAsync()
        {
            using var client = Client();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await client.Containers.KillContainerAsync(Kernel.Id, new ContainerKillParameters { Signal = "SIGKILL" }, timeout.Token)
                .WaitAsync(TimeSpan.FromSeconds(15));
        }

        internal async ValueTask DisposeFixtureAsync()
        {
            await Fixture!.DisposeAsync();
            _fixtureDisposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                Dispose = null;
                if (Fixture is not null && !_fixtureDisposed) await DisposeFixtureAsync();
            }
            finally
            {
                // Only resources reported by this scenario's hook are eligible for emergency cleanup.
                foreach (var container in _containers) await container.DisposeAsync();
                if (Material is not null) await Material.DisposeAfter(() => ValueTask.CompletedTask);
            }
        }

        internal static DockerClient Client() => TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid.NewGuid()).Build();

        static async Task AssertGone(string id)
        {
            using var client = Client();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var failure = await Catch.Exception(async () => await client.Containers.InspectContainerAsync(id, timeout.Token).WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.IsType<DockerContainerNotFoundException>(failure);
        }

        async ValueTask DisposeObserved(IContainer container, Func<ValueTask> realDispose)
        {
            foreach (var owned in _containers.Where(owned => !string.IsNullOrEmpty(owned.Id))) _createdIds.Add(owned.Id);
            var id = container.Id;
            if (Dispose is { } injected)
            {
                await injected(container, realDispose);
                return;
            }
            await realDispose();
            if (!string.IsNullOrEmpty(id)) await AssertGone(id);
            if (ReferenceEquals(container, Kernel) && Material is not null)
            {
                Directory.Exists(MaterialDirectory).ShouldBeTrue();
                KernelStopConfirmed = true;
            }
        }
    }
}
