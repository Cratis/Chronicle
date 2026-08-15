// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_recording_an_erasure;

/// <summary>
/// A fence recorded in one member only would be defeated by the composition itself, so it reaches every store - and
/// each one fences the material it actually holds, which is what makes the refusal specific to the key that was
/// destroyed rather than to the identifier in general.
/// </summary>
public class and_every_store_holds_the_key : given.two_key_stores
{
    EncryptionKey _key;
    EncryptionKeyErasure? _inPrimary;
    EncryptionKeyErasure? _inSecondary;
    EncryptionKeyErasure? _acrossTheComposite;

    async Task Establish()
    {
        _key = KeyNamed("shared");
        await Save(_primary, _key);
        await Save(_secondary, _key);
    }

    async Task Because()
    {
        await _composite.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _inPrimary = await _primary.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _inSecondary = await _secondary.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _acrossTheComposite = await _composite.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_fence_the_primary_store() => _inPrimary.ShouldNotBeNull();
    [Fact] void should_fence_the_secondary_store() => _inSecondary.ShouldNotBeNull();
    [Fact] void should_report_the_fence_across_the_composite() => _acrossTheComposite.ShouldNotBeNull();
    [Fact] void should_fence_the_destroyed_key_material() => _acrossTheComposite!.ErasedKeyFingerprints.ShouldContain(_key.Fingerprint);
    [Fact] void should_not_allow_a_new_key_by_default() => _acrossTheComposite!.NewKeyAllowed.ShouldBeFalse();
}
