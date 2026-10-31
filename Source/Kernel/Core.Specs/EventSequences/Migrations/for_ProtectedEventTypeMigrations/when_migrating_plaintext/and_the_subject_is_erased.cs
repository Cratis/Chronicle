// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_the_subject_is_erased : given.a_token_protected_only_in_the_latest_generation
{
    JsonObject[] _released;
    JsonObject[] _erased;
    JsonObject[] _stored;

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        var generations = _migrated.Select(pair => (Schema: GenerationProtectionSchemas.For(_definition, pair.Key), Content: pair.Value)).ToArray();
        _stored = generations.Select(pair => _converter.ToJsonObject(pair.Content, pair.Schema)).ToArray();
        _released = await Task.WhenAll(generations.Select((pair, index) => _manager.Release(Store, Namespace, pair.Schema, Subject, _stored[index])));
        await _keys.DeleteFor(Store, Namespace, Subject);
        _erased = await Task.WhenAll(generations.Select((pair, index) => _manager.Release(Store, Namespace, pair.Schema, Subject, _stored[index])));
    }

    [Fact] void should_protect_all_three_generations() => _stored.Length.ShouldEqual(3);
    [Fact] void should_release_the_token_before_erasure() => _released.All(content => content["token"]!.GetValue<string>() == Token).ShouldBeTrue();
    [Fact] void should_not_release_the_token_after_erasure() => _erased.All(content => content["token"]!.GetValue<string>() == string.Empty).ShouldBeTrue();
    [Fact] void should_not_store_plaintext_in_any_generation() => _stored.All(content => !content.ToJsonString().Contains(Token, StringComparison.Ordinal)).ShouldBeTrue();
}
