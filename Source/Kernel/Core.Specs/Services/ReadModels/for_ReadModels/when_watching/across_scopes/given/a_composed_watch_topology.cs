// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;
using Grpc.Core;
using Orleans.TestKit;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes.given;

/// <summary>
/// Composes the real watch path: the gRPC Watch, the real changeset mediator, real notifier and subscriber
/// grains, and real projection/reducer producers. Only Orleans routing is simulated, by grain key.
/// </summary>
public class a_composed_watch_topology : for_ReadModels.given.all_dependencies
{
    protected const string ObserverId = "test-observer";
    protected const string StoreS = "test-store";
    protected const string StoreT = "other-store";
    protected const string NamespaceA = "namespace-a";
    protected const string NamespaceB = "namespace-b";

    protected NotifierRouter _router = null!;
    internal ReadModels _readModels = null!;
    readonly List<CancellationTokenSource> _cancellations = [];
    readonly List<IDisposable> _subscriptions = [];

    void Establish()
    {
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };
        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema>
            {
                {
                    (Concepts.ReadModels.ReadModelGeneration)1,
                    new JsonSchema { Properties = { ["value"] = property } }
                }
            }
        };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _router = new NotifierRouter();
        _changesetMediator = new ProjectionChangesetMediator();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(call => _router.For(call.ArgAt<string>(0)));
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(call =>
            CreateSubscriber(call.ArgAt<string>(0)));

        _readModels = new ReadModels(
            _grainFactory,
            _storage,
            _readBindings,
            _expandoObjectConverter,
            _reducerMediator,
            _changesetMediator,
            _localSiloDetails,
            _complianceHelper,
            _materializedReadModels,
            new JsonSerializerOptions(),
            NamespaceCasingContext.Create());
        _service = _readModels;
    }

    protected void MakeReducerBacked() =>
        _readModel.GetDefinition().Returns(_readModelDefinition = _readModelDefinition with { ObserverType = Concepts.ReadModels.ReadModelObserverType.Reducer });

    ReadModelChangesetSubscriber CreateSubscriber(string key)
    {
        var silo = new TestKitSilo();
        silo.AddService(_changesetMediator);
        return silo.CreateGrainAsync<ReadModelChangesetSubscriber>(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Starts a watch and waits until it reports that it is subscribed.
    /// </summary>
    /// <param name="eventStore">The event store to watch.</param>
    /// <param name="namespace">The namespace to watch.</param>
    /// <returns>The started watch.</returns>
    protected async Task<Watch> StartWatch(string eventStore, string @namespace)
    {
        var watch = new Watch();
        var cancellation = new CancellationTokenSource();
        _cancellations.Add(cancellation);
        var subscribed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(_service.Watch(
                new WatchRequest { EventStore = eventStore, Namespace = @namespace, ReadModelIdentifier = "test-read-model", EventSequenceId = "event-log" },
                new CallContext(new CallOptions(cancellationToken: cancellation.Token)))
            .Subscribe(
                changeset =>
                {
                    if (changeset.Subscribed)
                    {
                        subscribed.TrySetResult();
                        return;
                    }

                    lock (watch.Received)
                    {
                        watch.Received.Add(changeset);
                    }
                },
                error => subscribed.TrySetException(error)));
        await subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        return watch;
    }

    /// <summary>
    /// Gets the compliance release calls made so far as (event store, namespace) pairs.
    /// </summary>
    /// <returns>The release calls.</returns>
    protected (string EventStore, string Namespace)[] ReleaseCalls() =>
        _complianceHelper.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "ReleaseJson")
            .Select(call => (call.GetArguments()[0]!.ToString()!, call.GetArguments()[1]!.ToString()!)).ToArray();

    protected static string[] Values(Watch watch)
    {
        lock (watch.Received)
        {
            return [.. watch.Received.Select(changeset => JsonNode.Parse(changeset.ReadModel)["value"].GetValue<string>())];
        }
    }

    void Destroy()
    {
        foreach (var cancellation in _cancellations)
        {
            cancellation.Cancel();
        }

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }

    protected class Watch
    {
        public List<ReadModelChangeset> Received { get; } = [];
    }
}
