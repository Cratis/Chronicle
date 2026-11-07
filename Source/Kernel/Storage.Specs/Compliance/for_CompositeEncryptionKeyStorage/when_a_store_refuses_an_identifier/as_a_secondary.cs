// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

public class as_a_secondary : conformance
{
    protected override CompositeEncryptionKeyStorage Compose(given.CapturingLogger<CompositeEncryptionKeyStorage> logger, IEncryptionKeyStorage refusing) =>
        new(logger, _primary, refusing);
}
