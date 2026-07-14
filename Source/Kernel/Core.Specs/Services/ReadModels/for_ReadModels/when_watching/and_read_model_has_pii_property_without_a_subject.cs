// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_read_model_has_pii_property_without_a_subject : given.all_dependencies
{
    IProjectionChangesetNotifier _notifier;
    TaskCompletionSource<IProjectionChangesetObserver> _observerCaptured;
    string? _releasedSubject;

    void Establish()
    {
        // Snapshot the subject the observable path resolves before it hands the document to ReleaseJson.
        _complianceHelper
            .ReleaseJson(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<JsonObject>())
            .Returns(callInfo =>
            {
                var released = callInfo.ArgAt<JsonObject>(3);
                _releasedSubject = released[WellKnownProperties.Subject]?.GetValue<string>();
                return Task.FromResult(released);
            });

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
                    new JsonSchema { Properties = { ["name"] = property } }
                }
            }
        };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _observerCaptured = new();
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(_notifier);
        _grainFactory
            .CreateObjectReference<IProjectionChangesetObserver>(Arg.Any<IProjectionChangesetObserver>())
            .Returns(ci => ci.Arg<IProjectionChangesetObserver>());

        _notifier.When(n => n.Subscribe(Arg.Any<IProjectionChangesetObserver>()))
            .Do(ci => _observerCaptured.SetResult(ci.Arg<IProjectionChangesetObserver>()));
        _notifier.Subscribe(Arg.Any<IProjectionChangesetObserver>()).Returns(Task.CompletedTask);
        _notifier.Unsubscribe(Arg.Any<IProjectionChangesetObserver>()).Returns(Task.CompletedTask);
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_ => { });

        var observer = await _observerCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The forwarded changeset carries no __subject — only the read model's own identifier. The observable
        // path must infer the compliance subject from it (matching the one-shot query paths) rather than
        // streaming the document back undecrypted.
        var model = new JsonObject
        {
            ["id"] = "key-1",
            ["name"] = "encrypted-name"
        };

        await observer.OnChangeset("test-namespace", "key-1", model);
    }

    [Fact] void should_release_compliance_metadata() => _complianceHelper.Received(1).ReleaseJson(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<JsonObject>());
    [Fact] void should_infer_the_subject_from_the_identifier() => _releasedSubject.ShouldEqual("key-1");
}
