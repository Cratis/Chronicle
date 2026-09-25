// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedGlobalValueHandler.given;

public class a_property_handler : Specification
{
    protected static readonly EncryptionKeyIdentifier KeyIdentifier = EncryptedValueKeyIdentifiers.ForGlobal();

    protected EncryptedGlobalValueHandler _handler;

    protected IManagedEncryptionKeyProvisioner _provisioner;
    protected IEncryptionKeyStorage _keyStore;
    protected IEncryption _encryption;
    protected EncryptionKey _key;

    void Establish()
    {
        _key = new EncryptionKey(Encoding.UTF8.GetBytes("PublicPart"), Encoding.UTF8.GetBytes("PrivatePart"));
        _keyStore = Substitute.For<IEncryptionKeyStorage>();
        _encryption = Substitute.For<IEncryption>();
        _provisioner = new ManagedEncryptionKeyProvisioner(_keyStore, _encryption);
        _handler = new(_provisioner, _keyStore, _encryption);

        // Stubbed under the fixed global event store/namespace pair - the handler is not given these, and must
        // never be called with whichever real (eventStore, eventStoreNamespace) the document it is protecting
        // actually belongs to.
        _keyStore.TryGetFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, KeyIdentifier)
            .Returns(Task.FromResult<EncryptionKey?>(_key));
    }
}
