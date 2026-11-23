// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.given;

/// <summary>
/// A pinned subscriber whose pipeline runs the real protection step with real encryption over a read model with a
/// [PII] and a namespace-encrypted property. Both hold plaintext shaped exactly like ciphertext (base64 of 256 bytes),
/// the shape a second release would mistake for a protected value. <see cref="ReleaseAsWatchDoes"/> releases the
/// notified document once, the way Watch does.
/// </summary>
public class a_change_for_a_model_with_protected_values : for_ProjectionObserverSubscriber.given.a_pinned_subscriber
{
    protected const string Source = "person-42";
    protected const string Owner = "subject-7";
    protected static readonly string CipherShaped = Convert.ToBase64String(new byte[256]);

    protected readonly InMemoryEncryptionKeyStorage _keys = new();
    protected ReadModelsCompliance _compliance;
    protected ExpandoObjectConverter _converter;
    protected ProjectionEventContext _context;
    protected JsonObject _document;
    protected ReadModelChangeContext _change;

    protected override JsonSchema ReadModelSchema() => JsonSchema.FromJson(
        """
        {"type":"object","properties":{
          "id":{"type":"string"},
          "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
          "secret":{"type":"string","security":[{"metadataType":"EncryptedNamespace","details":""}]}
        }}
        """);

    protected virtual Task BeforeProtecting() => Task.CompletedTask;

    protected virtual ProjectionEventContext WhatThePipelineReturns(ProjectionEventContext protectedContext) => protectedContext;

    protected Task<JsonObject> ReleaseAsWatchDoes() =>
        _compliance.ReleaseJson(_key.EventStore, _key.Namespace, ReadModelSchema(), _document, _change.Subject, _change.Subjects ?? new Dictionary<string, string>());

    async Task Establish()
    {
        _converter = new ExpandoObjectConverter(new TypeFormats());
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, _keys, encryption),
                new EncryptedNamespaceValueHandler(provisioner, _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new ReadModelsCompliance(manager, _converter);
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => _converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));

        var schema = ReadModelSchema();
        _projection.TargetReadModelSchema.Returns(schema);
        _event = _event with { Context = _event.Context with { EventType = _pin, EventSourceId = Source, Subject = (Subject)Owner } };
        var state = _converter.ToExpandoObject(new JsonObject { ["id"] = Source, ["name"] = CipherShaped, ["secret"] = CipherShaped }, schema);
        ((IDictionary<string, object?>)state)[WellKnownProperties.ReadModelInstanceInitialized] = true;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _event, state);

        await BeforeProtecting();
        var protection = new EncryptChangeset(_compliance, new ObjectComparer(), _key.EventStore, _key.Namespace);
        _context = await protection.Perform(_projection, new ProjectionEventContext(new Key(Source, ArrayIndexers.NoIndexers), _event, changeset, ProjectionOperationType.From, false));
        _cachedPipeline.Handle(_event).Returns(WhatThePipelineReturns(_context));

        _notifier.When(_ => _.Notify(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>()))
            .Do(call =>
            {
                _document = call.Arg<JsonObject>();
                _change = call.Arg<ReadModelChangeContext>();
            });
    }
}
