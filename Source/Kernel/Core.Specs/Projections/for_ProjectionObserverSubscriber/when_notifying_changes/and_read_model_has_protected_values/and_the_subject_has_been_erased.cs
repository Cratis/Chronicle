// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_read_model_has_protected_values;

public class and_the_subject_has_been_erased : given.a_change_for_a_model_with_protected_values
{
    JsonObject _released;

    protected override async Task BeforeProtecting()
    {
        await _keys.SaveFor(_key.EventStore, _key.Namespace, Owner, new Compliance.Encryption().GenerateKey());
        await _keys.RecordErasureFor(_key.EventStore, _key.Namespace, Owner);
        await _keys.DeleteFor(_key.EventStore, _key.Namespace, Owner);
    }

    async Task Because()
    {
        await _subscriber.OnNext("the-partition", [_event], new(null));
        _released = await ReleaseAsWatchDoes();
    }

    [Fact] void should_not_reveal_the_erased_personal_data() => _released["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_release_the_namespace_confidential_value_unchanged() => _released["secret"]!.GetValue<string>().ShouldEqual(CipherShaped);
}
