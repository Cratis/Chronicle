// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.given;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class a_release_with_personal_dictionary_keys : an_unrecoverable_value
{
    protected const string Identifier = "subject-6093@example.invalid";
    protected const string DictionaryKey = "contact-6093@example.invalid";
    protected CapturingLogger<JsonSchemaMetadataManager> _capturedLogger;
    protected JsonObject _result;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"contacts":{"type":"object","additionalProperties":{"type":"string","compliance":[{"metadataType":"test-metadata-type","details":""}]}}}}
            """);
        _input = new JsonObject { ["contacts"] = new JsonObject { [DictionaryKey] = "protected-value" } };
        _error = new InvalidOperationException($"Cannot release {Identifier} at {DictionaryKey}");
        _capturedLogger = new();
        var reporter = new UnrecoverableProtectedValueReporter(_capturedLogger, TimeProvider.System, () => _count++);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_valueHandler), _capturedLogger, reporter);
    }
}
