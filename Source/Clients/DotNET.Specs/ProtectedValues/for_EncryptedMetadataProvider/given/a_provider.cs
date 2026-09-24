// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.given;

public class a_provider : Specification
{
    protected EncryptedMetadataProvider provider;

    void Establish() => provider = new();
}
