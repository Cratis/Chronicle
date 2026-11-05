// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Naming.for_NamespaceOpeningGuard.given;

/// <summary>
/// A guard whose storage only completes on the scheduler that started the call, opened from a scheduler that runs
/// one thing at a time - the shape of a grain activation reading storage it has not opened yet.
/// </summary>
public class a_guard_on_a_single_threaded_scheduler : Specification
{
    protected static readonly EventStoreName EventStore = "store";
    protected static readonly EventStoreNamespaceName Namespace = "tenant";
    protected IResourceNameGate _gate;
    protected INamespaceStorage _namespaces;
    protected NamespaceOpeningGuard _guard;
    protected SingleThreadedScheduler _scheduler;

    void Establish()
    {
        _scheduler = new();
        _gate = Substitute.For<IResourceNameGate>();
        _gate.EnsureCanCreate(Arg.Any<ResourceCreation>()).Returns(_ => CompletingOnTheCaller(true));
        _namespaces = Substitute.For<INamespaceStorage>();
        _namespaces.GetAll().Returns(_ => CompletingOnTheCaller<IEnumerable<NamespaceState>>([]));
        _guard = new(_gate, () => CompletingOnTheCaller<IEnumerable<string>>([EventStore.Value]));
    }

    void Destroy() => _scheduler.Dispose();

    /// <summary>
    /// Runs the action on the single-threaded scheduler and gives up after a bound, so a hang fails the spec instead of
    /// the run.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>The failure, or null when the action completed.</returns>
    protected async Task<Exception?> OnTheScheduler(Action action) =>
        await Catch.Exception(() => _scheduler.Run(action).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));

    /// <summary>
    /// Returns a task that completes only on the scheduler that was current when it was asked for, as storage driver
    /// continuations captured on a grain scheduler do.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="value">The result.</param>
    /// <returns>The task.</returns>
    protected static Task<T> CompletingOnTheCaller<T>(T value)
    {
        var caller = SynchronizationContext.Current;
        if (caller is null)
        {
            return Task.FromResult(value);
        }

        var completion = new TaskCompletionSource<T>();
        ThreadPool.QueueUserWorkItem(_ => caller.Post(_ => completion.SetResult(value), null));
        return completion.Task;
    }

    /// <summary>
    /// A <see cref="SynchronizationContext"/> that runs everything on one dedicated thread, one item at a time.
    /// </summary>
    protected sealed class SingleThreadedScheduler : SynchronizationContext, IDisposable
    {
        readonly Channel<(SendOrPostCallback Callback, object? State)> _work = Channel.CreateUnbounded<(SendOrPostCallback Callback, object? State)>(new() { SingleReader = true });

        public SingleThreadedScheduler() => new Thread(Pump) { IsBackground = true, Name = "single-threaded-scheduler" }.Start();

        /// <inheritdoc/>
        /// <remarks>After disposal a hung run may still post work; nothing picks it up any more, so it is dropped.</remarks>
        public override void Post(SendOrPostCallback d, object? state) => _work.Writer.TryWrite((d, state));

        public Task Run(Action action)
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(
                _ =>
                {
                    try
                    {
                        action();
                        done.SetResult();
                    }
                    catch (Exception error)
                    {
                        done.SetException(error);
                    }
                },
                null);
            return done.Task;
        }

        public void Dispose() => _work.Writer.TryComplete();

        void Pump()
        {
            SetSynchronizationContext(this);
            while (_work.Reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
            {
                while (_work.Reader.TryRead(out var item))
                {
                    item.Callback(item.State);
                }
            }
        }
    }
}
