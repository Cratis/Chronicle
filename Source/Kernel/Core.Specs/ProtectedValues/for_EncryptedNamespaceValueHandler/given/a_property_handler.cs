// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedNamespaceValueHandler.given;

public class a_property_handler : Specification
{
    protected static readonly EventStoreName EventStore = "SomeEventStore";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "SomeNamespace";
    protected static readonly EncryptionKeyIdentifier KeyIdentifier = EncryptedValueKeyIdentifiers.ForNamespace();

    protected EncryptedNamespaceValueHandler _handler;

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

        // Stubbed under the fixed, no-per-document-component identity the handler builds internally - the whole
        // point of the namespace scope is that no subject/identifier participates in the key lookup at all.
        _keyStore.TryGetFor(EventStore, EventStoreNamespace, KeyIdentifier).Returns(Task.FromResult<EncryptionKey?>(_key));
    }
}
