// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.given;

public class a_property_handler : Specification
{
    protected const string Identifier = "39b34712-ad8e-4cde-b879-2719c995aa49";
    protected PIICompliancePropertyValueHandler _handler;

    protected IEncryptionKeyStorage _keyStore;
    protected IEncryption _encryption;
    protected EncryptionKey _key;

    void Establish()
    {
        _key = new EncryptionKey(Encoding.UTF8.GetBytes("PublicPart"), Encoding.UTF8.GetBytes("PrivatePart"));
        _keyStore = Substitute.For<IEncryptionKeyStorage>();
        _encryption = Substitute.For<IEncryption>();
        _handler = new(_keyStore, _encryption);
        _keyStore.GetFor(string.Empty, string.Empty, Identifier).Returns(Task.FromResult(_key));
    }
}
