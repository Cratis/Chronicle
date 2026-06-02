// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;
using ProjectionChangeset = Cratis.Chronicle.Projections.ProjectionChangeset;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_read_model_has_pii_property : given.all_dependencies
{
    IStreamProvider _streamProvider;
    IAsyncStream<ProjectionChangeset> _changesetStream;
    StreamSubscriptionHandle<ProjectionChangeset> _subscriptionHandle;
    TaskCompletionSource _observerCaptured;
    IAsyncObserver<ProjectionChangeset>? _capturedObserver;

    void Establish()
    {
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) } }
            }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema>
            {
                {
                    (Concepts.ReadModels.ReadModelGeneration)1,
                    new JsonSchema { Properties = { ["name"] = property } }
                }
            }
        };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _observerCaptured = new();
        _streamProvider = Substitute.For<IStreamProvider>();
        _changesetStream = Substitute.For<IAsyncStream<ProjectionChangeset>>();
        _subscriptionHandle = Substitute.For<StreamSubscriptionHandle<ProjectionChangeset>>();

        // GetStreamProvider resolves via ServiceProvider.GetRequiredKeyedService<IStreamProvider>(name).
        // Make ServiceProvider return the _clusterClient itself, which also implements IKeyedServiceProvider.
        _clusterClient.ServiceProvider.Returns((IServiceProvider)_clusterClient);
        var keyedClient = (IKeyedServiceProvider)_clusterClient;
        keyedClient.GetKeyedService(Arg.Any<Type>(), Arg.Any<object?>()).Returns(_streamProvider);
        keyedClient.GetRequiredKeyedService(Arg.Any<Type>(), Arg.Any<object?>()).Returns(_streamProvider);

        _streamProvider.GetStream<ProjectionChangeset>(Arg.Any<StreamId>()).Returns(_changesetStream);
        _subscriptionHandle.UnsubscribeAsync().Returns(Task.CompletedTask);

        // Set up all SubscribeAsync overloads — the exact one called depends on which Orleans extension is used
        Task<StreamSubscriptionHandle<ProjectionChangeset>> SubscribeHandler(CallInfo ci)
        {
            _capturedObserver = ci.Arg<IAsyncObserver<ProjectionChangeset>>();
            _observerCaptured.SetResult();
            return Task.FromResult(_subscriptionHandle);
        }

        _changesetStream.SubscribeAsync(Arg.Any<IAsyncObserver<ProjectionChangeset>>())
            .Returns(SubscribeHandler);
        _changesetStream.SubscribeAsync(Arg.Any<IAsyncObserver<ProjectionChangeset>>(), Arg.Any<StreamSequenceToken?>())
            .Returns(SubscribeHandler);
        _changesetStream.SubscribeAsync(Arg.Any<IAsyncObserver<ProjectionChangeset>>(), Arg.Any<StreamSequenceToken?>(), Arg.Any<string?>())
            .Returns(SubscribeHandler);

        _complianceManager
            .Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(ci => Task.FromResult(new JsonObject { ["name"] = "decrypted-value" }));
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_ => { });

        await _observerCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var model = new JsonObject
        {
            [WellKnownProperties.Subject] = "some-subject",
            ["name"] = "encrypted-name"
        };

        await _capturedObserver!.OnNextAsync(new ProjectionChangeset("test-namespace", "key-1", model));
    }

    [Fact] void should_call_compliance_manager_release() => _complianceManager.Received(1).Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), "some-subject", Arg.Any<JsonObject>());
}
