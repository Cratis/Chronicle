// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueHandler.given;

public class a_property_handler : Specification
{
    protected const string Identifier = "39b34712-ad8e-4cde-b879-2719c995aa49";
    protected static readonly EncryptionKeyIdentifier KeyIdentifier = EncryptedValueKeyIdentifiers.ForSubject(Identifier);

    protected EncryptedValueHandler _handler;

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

        // Stubbed under the disjoint key identity the handler builds internally - the bare Identifier a PII
        // handler would use is deliberately left unstubbed, so a spec that accidentally looks it up under the
        // bare subject fails loudly instead of silently passing against the wrong key.
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, KeyIdentifier).Returns(Task.FromResult<EncryptionKey?>(_key));
    }
}
