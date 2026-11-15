// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoders;

public class when_loading_for_two_event_stores : given.a_loader
{
    static readonly EventStoreName _firstStore = "first";
    static readonly EventStoreName _secondStore = "second";
    UniqueConstraintKeyedEncoder? _firstEncoder;
    UniqueConstraintKeyedEncoder? _secondEncoder;

    void Establish()
    {
        KeyStorageFor(_firstStore, UniqueConstraintEncodingKey.Create());
        KeyStorageFor(_secondStore, UniqueConstraintEncodingKey.Create());
    }

    async Task Because()
    {
        _firstEncoder = await _loader.TryGetFor(_firstStore);
        _secondEncoder = await _loader.TryGetFor(_secondStore);
    }

    [Fact] void should_read_each_key_once() => _keyStorages.Values.Count(_ => _.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IUniqueConstraintEncodingStorage.GetKey)) == 1).ShouldEqual(2);
    [Fact] void should_return_separate_encoders() => ReferenceEquals(_secondEncoder, _firstEncoder).ShouldBeFalse();
}
