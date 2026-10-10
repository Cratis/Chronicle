// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity.given;

public abstract class a_string_array_round_trip(MongoDBFixture fixture) : a_parity_scenario(fixture)
{
    static readonly string[]?[] _values = [[], ["first"], ["second"], ["third", "fourth"], ["fifth", "sixth"], [], null, ["final", "two"]];
    static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    readonly Cratis.Chronicle.Json.ExpandoObjectConverter _jsonConverter = new(new TypeFormats());
    int _reductions;
    protected InMemoryEncryptionKeyStorage _keys;
    protected IReadModelsCompliance _compliance;

    protected abstract string ArrayMetadata { get; }
    protected virtual string ItemMetadata => string.Empty;
    protected override bool ReleaseBeforeComparing => true;
    protected override bool ApplyStatesDuringEstablish => false;
    protected int Reductions => _reductions;

    protected override IReadOnlyList<Func<ExpandoObject>> States => _values
        .Select(values => (Func<ExpandoObject>)(() => Expando(("id", "root-1"), ("involvedUsers", values), ("assigneeLogins", values))))
        .ToArray();

    protected override JsonSchema CreateSchema() => JsonSchema.FromJson($$"""
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "involvedUsers": { "type": ["array", "null"], {{ArrayMetadata}} "items": { {{ItemMetadata}} "type": "string" } },
            "assigneeLogins": { "type": ["array", "null"], {{ArrayMetadata}} "items": { {{ItemMetadata}} "type": "string" } }
          }
        }
        """);

    protected override IReadModelsCompliance CreateCompliance()
    {
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _compliance = new ReadModelsCompliance(
            new JsonSchemaMetadataManager(
                new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)),
                NullLogger<JsonSchemaMetadataManager>.Instance),
            _jsonConverter);
        return _compliance;
    }

    protected override ExpandoObject Reduce(ExpandoObject? current, ExpandoObject next)
    {
        // Match the kernel's schema-based JSON transport and the client's typed deserialization before
        // invoking the reducer. A parity-only comparison can miss two sinks returning the same bad scalar.
        var model = current is null ? null : JsonSerializer.Deserialize<ArrayState>(_jsonConverter.ToJsonObject(current, CreateSchema()).ToJsonString(), _options);
        var index = _reductions++ / 2;
        if (index == 0)
        {
            model.ShouldBeNull();
        }
        else
        {
            model.ShouldNotBeNull();
            if (_values[index - 1] is null)
            {
                model!.InvolvedUsers.ShouldBeNull();
                model.AssigneeLogins.ShouldBeNull();
            }
            else
            {
                model!.InvolvedUsers.ShouldNotBeNull();
                model.AssigneeLogins.ShouldNotBeNull();
                model.InvolvedUsers!.ToArray().ShouldEqual(_values[index - 1]);
                model.AssigneeLogins!.ToArray().ShouldEqual(_values[index - 1]);
            }
        }

        return next;
    }

    sealed record ArrayState(IEnumerable<string>? InvolvedUsers, IEnumerable<string>? AssigneeLogins);
}
