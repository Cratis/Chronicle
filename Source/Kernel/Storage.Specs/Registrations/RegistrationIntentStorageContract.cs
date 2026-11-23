// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Registrations;

/// <summary>
/// Verifies the same registration intent storage contract against each provider.
/// </summary>
public abstract class RegistrationIntentStorageContract : Specification
{
    static readonly byte[] _payload = [0, 1, 2, 254, 255];
    static readonly DateTimeOffset _updatedAt = new(2026, 5, 17, 10, 30, 15, TimeSpan.Zero);
    IReadOnlyList<RegistrationIntent> _beforeAnything;
    IReadOnlyList<RegistrationIntent> _intents;

    /// <summary>
    /// Opens another storage instance over the same event-store database.
    /// </summary>
    /// <returns>The registration intent storage.</returns>
    protected abstract IRegistrationIntentStorage OpenStorage();

    async Task Because()
    {
        var storage = OpenStorage();
        _beforeAnything = await storage.GetAll();
        await storage.Save(Create("studio", 1, RegistrationIntentState.Pending, _payload, 3, "1.2.3", 0, null));
        await storage.Save(Create("other", 2, RegistrationIntentState.Pending, _payload, null, null, 1, "boom"));
        await OpenStorage().Save(Create("studio", 3, RegistrationIntentState.Applied, [], 3, "1.2.4", 0, null));
        await storage.Save(Create("studio|hash", 4, RegistrationIntentState.Pending, _payload, null, null, 0, null));
        _intents = await OpenStorage().GetAll();
    }

    [Fact] public void should_start_empty() => _beforeAnything.ShouldBeEmpty();
    [Fact] public void should_keep_one_intent_per_key() => _intents.Count.ShouldEqual(3);
    [Fact] public void should_replace_the_intent_with_the_same_key() => _intents.Single(_ => _.Key == "studio").Revision.ShouldEqual(3);
    [Fact] public void should_keep_the_state_of_the_replacement() => _intents.Single(_ => _.Key == "studio").State.ShouldEqual(RegistrationIntentState.Applied);
    [Fact] public void should_keep_the_version_of_the_replacement() => _intents.Single(_ => _.Key == "studio").Version.ShouldEqual("1.2.4");
    [Fact] public void should_drop_the_payload_of_the_replacement() => _intents.Single(_ => _.Key == "studio").Payload.ToArray().ShouldBeEmpty();
    [Fact] public void should_leave_the_other_intent_alone() => _intents.Single(_ => _.Key == "other").Revision.ShouldEqual(2);
    [Fact] public void should_keep_the_payload_bytes() => _intents.Single(_ => _.Key == "other").Payload.ToArray().ShouldEqual(_payload);
    [Fact] public void should_keep_the_attempts() => _intents.Single(_ => _.Key == "other").Attempts.ShouldEqual(1);
    [Fact] public void should_keep_the_failure() => _intents.Single(_ => _.Key == "other").Failure.ShouldEqual("boom");
    [Fact] public void should_keep_a_missing_full_set_owner() => _intents.Single(_ => _.Key == "other").FullSetOwner.ShouldBeNull();
    [Fact] public void should_keep_a_missing_version() => _intents.Single(_ => _.Key == "other").Version.ShouldBeNull();
    [Fact] public void should_keep_the_full_set_owner() => _intents.Single(_ => _.Key == "studio").FullSetOwner.ShouldEqual(3);
    [Fact] public void should_keep_when_it_last_changed() => _intents.Single(_ => _.Key == "other").UpdatedAt.ShouldEqual(_updatedAt);
    [Fact] public void should_keep_the_registrant() => _intents.Single(_ => _.Key == "studio|hash").Registrant.ShouldEqual("studio|hash");
    [Fact] public void should_keep_the_hash() => _intents.Single(_ => _.Key == "studio|hash").Hash.ShouldEqual("hash");

    static RegistrationIntent Create(string key, long revision, RegistrationIntentState state, byte[] payload, int? fullSetOwner, string? version, int attempts, string? failure) =>
        new(RegistrationKind.Projections, key, key, revision, "hash", state, payload, fullSetOwner, version, attempts, failure, _updatedAt);
}
