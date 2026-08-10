// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

public class when_composing_no_stores : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
    {
        // A composite with nothing to compose accepts every key and holds none of them, so a value could be
        // protected under a key that was never persisted and can never be read back.
        _ = new CompositeEncryptionKeyStorage();
    });

    [Fact] void should_fail() => _error.ShouldBeOfExactType<MissingInnerEncryptionKeyStorage>();
}
