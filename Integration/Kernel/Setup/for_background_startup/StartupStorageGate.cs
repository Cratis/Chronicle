// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Kernel.Integration.Setup.for_background_startup;

public class StartupStorageGate
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    const string StartupTaskTypeName = "Orleans.Hosting.ChronicleServerStartupTask";
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _armed;

    public Task Entered => _entered.Task;
    public bool IsReleased => _released.Task.IsCompleted;
    public bool Expired { get; private set; }
    public void Arm() => _armed = true;
    public void Release() => _released.TrySetResult();

    public IStorage Wrap(IStorage storage) => ForwardingProxy.Wrap(storage, (method, args, result) =>
        method == nameof(IStorage.GetEventStore) && args![0]!.ToString() == BackgroundStartupFixture.EventStore
            ? WrapStore((IEventStoreStorage)result!) : result);

    IEventStoreStorage WrapStore(IEventStoreStorage store) => ForwardingProxy.Wrap(store, (method, args, result) =>
        method == nameof(IEventStoreStorage.GetNamespace) && args![0]!.ToString() == EventStoreNamespaceName.Default.Value
            ? WrapNamespace((IEventStoreNamespaceStorage)result!) : result);

    IEventStoreNamespaceStorage WrapNamespace(IEventStoreNamespaceStorage storage) => ForwardingProxy.Wrap(storage, (method, _, result) =>
        method == "get_Observers"
            ? ForwardingProxy.Wrap((IObserverStateStorage)result!, (member, _, value) =>
                member == nameof(IObserverStateStorage.GetAll) && _armed && !IsReleased && IsCalledByStartupTask()
                    ? Hold((Task<IEnumerable<ObserverState>>)value!) : value)
            : result);

    /// <summary>
    /// Whether the startup task issued the current read. Only its own recovery read is held: client-facing reads of the
    /// same storage (for example GetObservers, which a connecting client's identity-drift check calls) must pass through,
    /// as they do in production. The read is issued synchronously from the startup task's async method, so its state
    /// machine is on the stack.
    /// </summary>
    /// <returns>True when a frame of the startup task is on the call stack.</returns>
    static bool IsCalledByStartupTask() => new StackTrace().GetFrames().Any(frame =>
        frame.GetMethod()?.DeclaringType?.FullName is { } name &&
        (name == StartupTaskTypeName || name.StartsWith($"{StartupTaskTypeName}+", StringComparison.Ordinal)));

    async Task<IEnumerable<ObserverState>> Hold(Task<IEnumerable<ObserverState>> read)
    {
        // Materialize the real pre-registration snapshot, not a deferred or fabricated enumeration.
        var snapshot = (await read.WaitAsync(BackgroundStartupFixture.BarrierDeadline)).ToArray();
        _entered.TrySetResult();
        try
        {
            await _released.Task.WaitAsync(BackgroundStartupFixture.HoldDeadline);
        }
        catch (TimeoutException)
        {
            Expired = true;
            throw;
        }

        return snapshot;
    }

    public class ForwardingProxy : DispatchProxy
    {
        object _target = null!;
        Func<string, object?[]?, object?, object?> _observe = null!;

        public static T Wrap<T>(T target, Func<string, object?[]?, object?, object?> observe)
            where T : class
        {
            var proxy = Create<T, ForwardingProxy>();
            var state = (ForwardingProxy)(object)proxy;
            state._target = target;
            state._observe = observe;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            try
            {
                return _observe(targetMethod!.Name, args, targetMethod.Invoke(_target, args));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
