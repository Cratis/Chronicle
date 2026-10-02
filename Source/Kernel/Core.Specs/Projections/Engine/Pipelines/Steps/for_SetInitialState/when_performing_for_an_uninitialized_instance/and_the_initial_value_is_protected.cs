// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_an_uninitialized_instance;

public class and_the_initial_value_is_protected : given.a_set_initial_state_step
{
    ReadModelsCompliance _compliance;
    ProjectionEventContext _context;
    Exception? _error;
    PropertyDifference[] _differences = [];

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "secret":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}
            }}
            """);
        _projection.TargetReadModelSchema.Returns(schema);
        dynamic initial = new ExpandoObject();
        initial.secret = "initial-secret";
        _projection.InitialModelState.Returns((ExpandoObject)initial);
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        _compliance = new ReadModelsCompliance(new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance), new ExpandoObjectConverter(new TypeFormats()));
        dynamic placeholder = new ExpandoObject();
        placeholder.id = "the-key";
        placeholder.name = "existing-name";
        placeholder.__initialized = false;
        var stored = await _compliance.Apply("store", "namespace", schema, "the-key", placeholder);
        _sink.FindOrDefault(Arg.Any<Key>()).Returns((ExpandoObject)stored);
        _context = CreateContext(ProjectionOperationType.From);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        _context = await _step.Perform(_projection, _context);
        _context = await new DecryptInitialState(_compliance, "store", "namespace").Perform(_projection, _context);
        _context = await new EncryptChangeset(_compliance, new ObjectComparer(), "store", "namespace").Perform(_projection, _context);
        _differences = _context.Changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(change => change.Differences).ToArray();
    });

    [Fact] void should_release_and_reencrypt_without_error() => _error.ShouldBeNull();
    [Fact] void should_only_persist_the_encrypted_initial_value() => _differences.Single(difference => difference.PropertyPath.Path == "secret").Changed.ShouldNotEqual("initial-secret");
    [Fact] void should_only_persist_the_reencrypted_stored_value() => _differences.Single(difference => difference.PropertyPath.Path == "name").Changed.ShouldNotEqual("existing-name");
    [Fact] void should_release_the_stored_value() => ((IDictionary<string, object?>)_context.Changeset.CurrentState)["name"].ShouldEqual("existing-name");
    [Fact] void should_keep_the_initial_value() => ((IDictionary<string, object?>)_context.Changeset.CurrentState)["secret"].ShouldEqual("initial-secret");
    [Fact] void should_keep_the_initialization_marker_for_bulk_reads() => ((IDictionary<string, object?>)_context.Changeset.CurrentState)[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
}
