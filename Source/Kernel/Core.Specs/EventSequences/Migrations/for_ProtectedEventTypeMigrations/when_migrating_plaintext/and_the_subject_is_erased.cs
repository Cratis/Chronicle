// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_the_subject_is_erased : given.a_token_protected_only_in_the_latest_generation
{
    JsonObject _released;
    JsonObject _erased;

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        var schema = GenerationProtectionSchemas.For(_definition, 1);
        var content = _converter.ToJsonObject(_migrated[(Concepts.Events.EventTypeGeneration)1], schema);
        _released = await _manager.Release(Store, Namespace, schema, Subject, content);
        await _keys.DeleteFor(Store, Namespace, Subject);
        _erased = await _manager.Release(Store, Namespace, schema, Subject, content);
    }

    [Fact] void should_release_the_token_before_erasure() => _released["token"].GetValue<string>().ShouldEqual(Token);
    [Fact] void should_not_release_the_token_after_erasure() => _erased["token"].GetValue<string>().ShouldEqual(string.Empty);
}
