// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Threading.Channels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Integration.for_Reactors.when_the_kernel_restarts;

/// <summary>
/// Reproduces Cratis/Chronicle#5855: a reactor registered with a delegate on a client using automatic registration
/// must observe events appended after the kernel process has been restarted underneath the connected client.
/// </summary>
public class and_a_delegate_reactor_is_registered : Cratis.Specifications.Specification
{
    static readonly ReactorId _reactorId = new("kernel-restart-delegate-reactor");

    [Fact]
    public async Task should_receive_an_event_appended_after_the_kernel_is_back()
    {
        var options = ChronicleRuntimeOptions.Parse();
        Assert.SkipUnless(
            options.Mode == ChronicleRuntimeMode.OutOfProcess && options.StorageProvider == ChronicleStorageProvider.PostgreSql,
            "Kernel restart proofs run in the outofprocess postgresql client matrix lane.");

        await using var kernel = new SqlProcessKernel();
        await kernel.Start();

        using var client = new ChronicleClient(
            new ChronicleOptions { ConnectionString = new(kernel.Connection) },
            artifactsProvider: new Artifacts());
        var store = await client.GetEventStore(kernel.Store);
        (await store.WaitForRegistration(TimeSpan.FromSeconds(60))).IsSuccess.ShouldBeTrue();

        var received = Channel.CreateUnbounded<string>();
        await store.Reactors.Register(
            _reactorId,
            definition => definition.WithEventType(store.EventTypes.GetEventTypeFor(typeof(KernelRestartHappened))),
            (@event, _) =>
            {
                received.Writer.TryWrite(@event.Content.ToJsonString());
                return Task.CompletedTask;
            });

        await AppendUntilAccepted(store, "before");
        (await Next(received, TimeSpan.FromSeconds(60))).ShouldContain("before");

        await kernel.KillKernel();
        await kernel.RestartKernel();

        await AppendUntilAccepted(store, "after");
        var afterRestart = await NextContaining(received, "after", TimeSpan.FromSeconds(90));
        var state = await store.Reactors.GetHandlerById(_reactorId).GetState();

        afterRestart.ShouldContain("after");
        state.IsSubscribed.ShouldBeTrue();
    }

    static async Task AppendUntilAccepted(IEventStore store, string value)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (true)
        {
            try
            {
                var result = await store.EventLog.Append(Guid.NewGuid().ToString("N"), new KernelRestartHappened(value));
                if (result.IsSuccess)
                {
                    return;
                }
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                // The client is still reconnecting to the restarted kernel.
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Appending '{value}' was not accepted within 90 seconds of the kernel restart.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    static async Task<string> NextContaining(Channel<string> received, string value, TimeSpan timeout)
    {
        // Killing the kernel can lose the observer's progress, so events from before the restart may be redelivered.
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var remaining = deadline - DateTime.UtcNow;
            var next = await Next(received, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(1));
            if (next.Contains(value, StringComparison.Ordinal))
            {
                return next;
            }
        }

        return "<nothing received within the deadline>";
    }

    static async Task<string> Next(Channel<string> received, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            return await received.Reader.ReadAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return "<nothing received within the deadline>";
        }
    }

    [EventType]
    public record KernelRestartHappened(string Value);

    sealed class Artifacts() : DefaultClientArtifactsProvider(new Discovery());

    sealed class Discovery : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [typeof(Discovery).Assembly];
        public IEnumerable<Type> DefinedTypes => [typeof(KernelRestartHappened)];

        public void Initialize()
        {
        }
    }
}
