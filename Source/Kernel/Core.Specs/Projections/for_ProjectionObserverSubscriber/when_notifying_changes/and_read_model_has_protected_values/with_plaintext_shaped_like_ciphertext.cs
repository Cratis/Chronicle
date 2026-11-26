// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_read_model_has_protected_values;

public class with_plaintext_shaped_like_ciphertext : given.a_change_for_a_model_with_protected_values
{
    JsonObject _released;

    async Task Because()
    {
        await _subscriber.OnNext("the-partition", [_event], new(null));
        _released = await ReleaseAsWatchDoes();
    }

    [Fact] void should_notify_the_personal_data_protected() => _document["name"]!.GetValue<string>().ShouldNotEqual(CipherShaped);
    [Fact] void should_notify_the_namespace_confidential_value_protected() => _document["secret"]!.GetValue<string>().ShouldNotEqual(CipherShaped);
    [Fact] void should_carry_the_event_subject_as_lineage() => _change.Subject.ShouldEqual(Owner);
    [Fact] void should_release_the_personal_data_unchanged_in_a_single_release() => _released["name"]!.GetValue<string>().ShouldEqual(CipherShaped);
    [Fact] void should_release_the_namespace_confidential_value_unchanged_in_a_single_release() => _released["secret"]!.GetValue<string>().ShouldEqual(CipherShaped);
    [Fact] void should_keep_the_current_state_in_plaintext_for_the_next_event() => ((IDictionary<string, object?>)_context.Changeset.CurrentState)["name"].ShouldEqual(CipherShaped);
}
