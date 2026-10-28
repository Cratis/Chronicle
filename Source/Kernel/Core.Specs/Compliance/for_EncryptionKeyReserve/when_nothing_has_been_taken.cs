// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyReserve;

public class when_nothing_has_been_taken : Specification
{
    int _generated;

    async Task Because()
    {
        _ = new EncryptionKeyReserve(
            () =>
            {
                Interlocked.Increment(ref _generated);
                return new EncryptionKey([1], [1]);
            },
            4);
        await Task.Delay(50);
    }

    [Fact] void should_not_generate_any_key() => _generated.ShouldEqual(0);
}
