// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Compliance.for_PIICompliancePropertyValueHandler.given
{
    public class a_property_handler : Specification
    {
        protected const string identifier = "39b34712-ad8e-4cde-b879-2719c995aa49";
        protected PIICompliancePropertyValueHandler handler;

        protected Mock<IEncryptionKeyStore> key_store;
        protected Mock<IEncryption> encryption;
        protected EncryptionKey key;

        void Establish()
        {
            key = new EncryptionKey(Encoding.UTF8.GetBytes("TheKey"));
            key_store = new();
            encryption = new();
            handler = new(key_store.Object, encryption.Object);
            key_store.Setup(_ => _.GetFor(identifier)).Returns(Task.FromResult(key));
        }
    }
}
