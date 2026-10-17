// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.when_recording;

public class against_an_old_kernel_with_triggers : given.a_store
{
    Exception? _recordError;
    Exception? _lookupError;

    void Establish() => _store.Capabilities.Old();

    async Task Because()
    {
        _recordError = await Catch.Exception(() => _store.Occurrences.RecordEffectReceipt(_key, new NoEvents()));
        _lookupError = await Catch.Exception(() => _store.Occurrences.GetEffectReceipt(_key));
    }

    [Fact] void should_refuse_recording_without_attempting_trigger_registration() => _recordError.ShouldBeOfExactType<EffectReceiptsNotSupported>();
    [Fact] void should_refuse_lookup_without_attempting_trigger_registration() => _lookupError.ShouldBeOfExactType<EffectReceiptsNotSupported>();
}
