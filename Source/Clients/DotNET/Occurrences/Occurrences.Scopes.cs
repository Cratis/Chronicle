// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

public partial class Occurrences
{
    async Task RunScopedWorkers(CancellationToken cancellationToken)
    {
        var running = new Dictionary<(Type Trigger, OccurrenceDeliveryScope Scope), Task>();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var pair in _triggers!.Handlers)
                {
                    OccurrenceDeliveryScope[] scopes;
                    lock (_workerSync)
                    {
                        if (!TriggerRegistry.IsLifecycle(pair.Trigger)) scopes = [new(false, eventStore.Namespace)];
                        else if (_options.LifecycleScope == LifecycleScope.Application) scopes = [new(true, null)];
                        else scopes = _lifecycleNamespaces.Select(name => new OccurrenceDeliveryScope(false, name)).ToArray();
                    }
                    foreach (var scope in scopes)
                    {
                        if (running.ContainsKey((pair.Trigger, scope))) continue;
                        var worker = new OccurrenceWorker(eventStore, _triggers.Get(pair.Trigger).Id, _attemptFactory!(pair.Trigger, pair.Handler, scope), TimeProvider, _logger!, scope);
                        running[(pair.Trigger, scope)] = Task.Run(() => worker.Run(cancellationToken), CancellationToken.None);
                    }
                }
                await _scopeChanges.Reader.ReadAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Session cancellation joins every namespace worker before reconnect can start another session.
        }
        finally
        {
            await Task.WhenAll(running.Values);
        }
    }

    void AddLifecycleNamespaces(IEnumerable<string> namespaces)
    {
        lock (_workerSync)
        {
            _lifecycleNamespaces.UnionWith(namespaces);
            _scopeChanges.Writer.TryWrite(true);
        }
    }
}
