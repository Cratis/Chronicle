// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class and_nothing_secret_is_logged_or_appended : given.a_signing_key_ring
{
    readonly List<WebhookSigningKey> _issued = [];

    async Task Establish() => await CreateRing();

    async Task Because()
    {
        _issued.Add(await _ring.RotateWebhookSecret("webhook", WebhookSigningKeyOverlap.Default));
        _issued.Add(await _ring.RotateWebhookSecret("webhook", WebhookSigningKeyOverlap.Default));
        await _ring.RetireWebhookSecrets("webhook");
    }

    [Fact] void should_exercise_generated_superseded_and_retired_events() => _appended.Select(entry => entry.Event.GetType()).Distinct().Count().ShouldEqual(3);
    [Fact] void should_record_logs() => _logger.Entries.ShouldNotBeEmpty();
    [Fact] void should_not_append_material() => _appended.Exists(entry => ContainsMaterial(JsonSerializer.Serialize(entry.Event, entry.Event.GetType()))).ShouldBeFalse();
    [Fact] void should_not_log_material() => _logger.Entries.Exists(ContainsMaterial).ShouldBeFalse();
    [Fact] void should_not_include_material_in_string_representations() => _issued.Exists(key => ContainsMaterial(key.ToString())).ShouldBeFalse();

    bool ContainsMaterial(string text) => text.Contains("whsec_", StringComparison.Ordinal) || _encrypted.Exists(secret => text.Contains(secret, StringComparison.Ordinal)) || _issued.Exists(key => text.Contains(key.EncryptedSecret.Value, StringComparison.Ordinal));
}
