// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Encodes and decodes the at-rest shape a managed <see cref="EncryptionKey"/> produces for a JSON value - the
/// base64 ciphertext string every <see cref="IJsonSchemaMetadataValueHandler"/> built on
/// <see cref="IEncryption"/> stores in place of the original value.
/// </summary>
/// <remarks>
/// Extracted verbatim from <see cref="Compliance.GDPR.PIICompliancePropertyValueHandler"/>, where this reasoning
/// was first worked out and hardened. It is shared rather than re-derived per feature because the order of the
/// checks in <see cref="TryDecodeCipherText"/> is the part that is easy to get wrong a second time: the shape
/// check has to come before any key lookup, so that a value which was never protected - because it predates the
/// property being marked, or because it is resolved in memory at the query edge for display - is recognized
/// without needing a key at all, and releasing it stays a no-op instead of a key-store round trip that can only
/// fail. Nothing here knows about subjects, GDPR, or any other policy - it is the same codec whether the caller is
/// PII compliance or plain-confidentiality encryption.
/// </remarks>
public static class ProtectedValueCodec
{
    /// <summary>
    /// Encrypt a JSON value under a managed <see cref="EncryptionKey"/>, producing the base64 ciphertext
    /// <see cref="JsonNode"/> stored in its place.
    /// </summary>
    /// <param name="encryption"><see cref="IEncryption"/> to encrypt with.</param>
    /// <param name="key"><see cref="EncryptionKey"/> to encrypt under.</param>
    /// <param name="value"><see cref="JsonNode"/> to encrypt.</param>
    /// <returns>The encrypted value, as a base64 <see cref="JsonValue"/>.</returns>
    public static JsonNode Encrypt(IEncryption encryption, EncryptionKey key, JsonNode value)
    {
        var valueAsString = value.ToString();
        var encrypted = encryption.Encrypt(Encoding.UTF8.GetBytes(valueAsString), key);
        var encryptedAsBase64 = Convert.ToBase64String(encrypted);
        return JsonValue.Create(encryptedAsBase64);
    }

    /// <summary>
    /// Decrypt ciphertext bytes under a managed <see cref="EncryptionKey"/>, producing the original JSON value.
    /// </summary>
    /// <param name="encryption"><see cref="IEncryption"/> to decrypt with.</param>
    /// <param name="key"><see cref="EncryptionKey"/> to decrypt under.</param>
    /// <param name="encrypted">The ciphertext bytes previously decoded by <see cref="TryDecodeCipherText"/>.</param>
    /// <returns>The decrypted value, as a <see cref="JsonValue"/>.</returns>
    public static JsonNode Decrypt(IEncryption encryption, EncryptionKey key, byte[] encrypted)
    {
        var decrypted = encryption.Decrypt(encrypted, key);
        var decryptedAsString = Encoding.UTF8.GetString(decrypted);
        return JsonValue.Create(decryptedAsString);
    }

    /// <summary>
    /// Try to decode a string value as ciphertext <see cref="Encrypt"/> could have produced.
    /// </summary>
    /// <param name="encryption"><see cref="IEncryption"/> whose shape the bytes are checked against.</param>
    /// <param name="value">The string value to check.</param>
    /// <param name="encrypted">The decoded ciphertext bytes, when the value carries the expected shape.</param>
    /// <returns>True when the value could have been produced by <see cref="Encrypt"/>, false otherwise.</returns>
    /// <remarks>
    /// Only a value this encryption produced can be decoded here. One that carries none of its shape was never
    /// encrypted under a managed key - it is resolved in memory at the query edge for display, or it predates the
    /// property being marked. Asking the shape before any key lookup is deliberate: whether a key exists answers
    /// "can this be decrypted", and the question here is "was this ever encrypted" - a question that must be
    /// answerable without needing a key at all, or a value with no key (a read model keyed by a hash, a cluster
    /// identifier, or any other computed identity that never provisions one) would have its plain values emptied
    /// on every release.
    /// </remarks>
    public static bool TryDecodeCipherText(IEncryption encryption, string value, out byte[] encrypted)
    {
        encrypted = [];

        // Base64 encodes four characters per three bytes, so anything else cannot be a value Encrypt produced.
        if (value.Length == 0 || value.Length % 4 != 0)
        {
            return false;
        }

        var buffer = new byte[value.Length / 4 * 3];
        if (!Convert.TryFromBase64String(value, buffer, out var bytesWritten))
        {
            return false;
        }

        var decoded = buffer[..bytesWritten];
        if (!encryption.IsEncrypted(decoded))
        {
            return false;
        }

        encrypted = decoded;
        return true;
    }
}
