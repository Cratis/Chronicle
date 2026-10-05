// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

/// <summary>
/// Reuses real ciphertext vectors across differential runs: random ciphertext in conversion errors must
/// compare equally, and thousands of schema cells do not need to repeat the same RSA operation.
/// </summary>
public class encryption_vectors : IEncryption
{
    readonly Encryption _encryption = new();
    readonly EncryptionKey _proposedKey = new Encryption().GenerateKey();
    readonly ConcurrentDictionary<(string Key, string Bytes), byte[]> _encrypted = new();
    readonly ConcurrentDictionary<(string Key, string Bytes), byte[]> _decrypted = new();

    public EncryptionKey GenerateKey() => _proposedKey;
    public bool IsEncrypted(byte[] bytes) => _encryption.IsEncrypted(bytes);
    public byte[] Encrypt(byte[] bytes, EncryptionKey key) => _encrypted.GetOrAdd((key.Fingerprint, Convert.ToBase64String(bytes)), static (_, args) => args.Encryption.Encrypt(args.Bytes, args.Key), (Encryption: _encryption, Bytes: bytes, Key: key));
    public byte[] Decrypt(byte[] bytes, EncryptionKey key) => _decrypted.GetOrAdd((key.Fingerprint, Convert.ToBase64String(bytes)), static (_, args) => args.Encryption.Decrypt(args.Bytes, args.Key), (Encryption: _encryption, Bytes: bytes, Key: key));
}
