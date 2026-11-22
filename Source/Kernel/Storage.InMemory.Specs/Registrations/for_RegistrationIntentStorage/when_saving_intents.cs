// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Storage.InMemory.Registrations.for_RegistrationIntentStorage;

public class when_saving_intents : Specification
{
    RegistrationIntentStorage _storage;
    IReadOnlyList<RegistrationIntent> _intents;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(CreateIntent("studio", 1, RegistrationIntentState.Pending));
        await _storage.Save(CreateIntent("other", 2, RegistrationIntentState.Pending));
        await _storage.Save(CreateIntent("studio", 3, RegistrationIntentState.Applied));
        _intents = await _storage.GetAll();
    }

    [Fact] void should_keep_one_intent_per_key() => _intents.Count.ShouldEqual(2);
    [Fact] void should_replace_the_intent_with_the_same_key() => _intents.Single(intent => intent.Key == "studio").Revision.ShouldEqual(3);
    [Fact] void should_keep_the_state_of_the_replacement() => _intents.Single(intent => intent.Key == "studio").State.ShouldEqual(RegistrationIntentState.Applied);
    [Fact] void should_leave_the_other_intent_alone() => _intents.Single(intent => intent.Key == "other").Revision.ShouldEqual(2);

    static RegistrationIntent CreateIntent(string key, long revision, RegistrationIntentState state) =>
        new(RegistrationKind.Projections, key, key, revision, "hash", state, ReadOnlyMemory<byte>.Empty, null, null, 0, null, DateTimeOffset.UnixEpoch);
}
