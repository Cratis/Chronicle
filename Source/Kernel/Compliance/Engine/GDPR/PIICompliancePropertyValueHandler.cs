// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Newtonsoft.Json.Linq;

namespace Aksio.Cratis.Compliance.GDPR
{
    /// <summary>
    /// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for handling PII.
    /// </summary>
    public class PIICompliancePropertyValueHandler : IJsonCompliancePropertyValueHandler
    {
        readonly IEncryptionKeyStore _encryptionKeyStore;
        readonly IEncryption _encryption;

        /// <inheritdoc/>
        public ComplianceMetadataType Type => ComplianceMetadataType.PII;

        /// <summary>
        /// Initializes a new instance of the <see cref="PIICompliancePropertyValueHandler"/>.
        /// </summary>
        /// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStore"/> to use for keys.</param>
        /// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
        public PIICompliancePropertyValueHandler(IEncryptionKeyStore encryptionKeyStore, IEncryption encryption)
        {
            _encryptionKeyStore = encryptionKeyStore;
            _encryption = encryption;
        }

        /// <inheritdoc/>
        public async Task<JToken> Apply(string identifier, JToken value)
        {
            var key = await _encryptionKeyStore.GetFor(identifier);
            var valueAsString = value.ToString();
            var encrypted = _encryption.Encrypt(Encoding.UTF8.GetBytes(valueAsString), key);
            var encryptedAsBase64 = Convert.ToBase64String(encrypted);
            return JToken.FromObject(encryptedAsBase64);
        }

        /// <inheritdoc/>
        public async Task<JToken> Release(string identifier, JToken value)
        {
            var key = await _encryptionKeyStore.GetFor(identifier);
            var encryptedAsString = value.ToString();
            var encrypted = Convert.FromBase64String(encryptedAsString);
            var decrypted = _encryption.Decrypt(encrypted, key);
            var decryptedAsString = Encoding.UTF8.GetString(decrypted);
            return JToken.FromObject(decryptedAsString);
        }
    }
}
