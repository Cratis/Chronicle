// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationKeyProtection"/> that wraps keys with AES-256-GCM under a key-encryption key.
/// </summary>
/// <remarks>
/// <para>
/// The key-encryption key is read from the file at <see cref="FederationHostedOptions.KeyEncryptionKeyPath"/> and never from configuration, logs or events.
/// When that is not set, the kernel only continues if the key custody genuinely keeps internal secrets outside the event store databases.
/// Otherwise it fails closed. A Development environment may generate the file when it is missing; production never does.
/// </para>
/// <para>
/// A wrapped key is <c language="text">version || nonce || tag || ciphertext</c>, and the purpose, identity and serial number are the
/// associated data, so a wrapped key cannot be moved to another identity or certificate.
/// </para>
/// </remarks>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the hosted federation configuration.</param>
/// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
/// <param name="custody">The <see cref="IEncryptionKeyStorage"/> that is the kernel's key custody.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public sealed class FederationKeyProtection(
    IOptions<ChronicleOptions> options,
    IHostEnvironment environment,
    IEncryptionKeyStorage custody,
    ILogger<FederationKeyProtection> logger) : IFederationKeyProtection, IDisposable
{
    const byte Version = 2;
    const int KeySize = 32;
    const int NonceSize = 12;
    const int TagSize = 16;

    readonly SemaphoreSlim _lock = new(1, 1);
    byte[]? _key;
    byte[]? _previousKey;
    bool _previousLoaded;

    /// <inheritdoc/>
    public void Dispose()
    {
        _lock.Dispose();
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
        }

        if (_previousKey is not null)
        {
            CryptographicOperations.ZeroMemory(_previousKey);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> Protect(byte[] plaintext, string purpose, string identity, string serial)
    {
        var key = await GetKey();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag, AssociatedData(purpose, identity, serial));
        return [Version, .. nonce, .. tag, .. cipher];
    }

    /// <inheritdoc/>
    public async Task<byte[]> Unprotect(byte[] wrapped, string purpose, string identity, string serial)
    {
        if (wrapped.Length < 1 + NonceSize + TagSize || wrapped[0] != Version)
        {
            throw new CryptographicException("The wrapped key is not in a format this kernel wrote.");
        }

        var associatedData = AssociatedData(purpose, identity, serial);
        try
        {
            return Decrypt(wrapped, await GetKey(), associatedData);
        }
        catch (CryptographicException)
        {
            var previous = await GetPreviousKey();
            if (previous is null)
            {
                throw;
            }

            return Decrypt(wrapped, previous, associatedData);
        }
    }

    static byte[] Decrypt(byte[] wrapped, byte[] key, byte[] associatedData)
    {
        var nonce = wrapped.AsSpan(1, NonceSize);
        var tag = wrapped.AsSpan(1 + NonceSize, TagSize);
        var cipher = wrapped.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext, associatedData);
        return plaintext;
    }

    static byte[] AssociatedData(string purpose, string identity, string serial) =>
        Encoding.UTF8.GetBytes($"chronicle-federation-key-v2|{purpose}|{identity}|{serial}");

    static byte[] ReadKeyFile(string path)
    {
        var key = new byte[KeySize];
        string text;
        try
        {
            text = File.ReadAllText(path).Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new FederationKeyEncryptionKeyUnavailable($"the key-encryption key file could not be read ({ex.GetType().Name})");
        }

        if (!Convert.TryFromBase64String(text, key, out var written) || written != KeySize)
        {
            throw new FederationKeyEncryptionKeyUnavailable($"the key-encryption key file must hold exactly {KeySize} bytes, base64 encoded");
        }

        return key;
    }

    static void Generate(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fileOptions = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        var key = RandomNumberGenerator.GetBytes(KeySize);
        try
        {
            using var stream = new FileStream(path, fileOptions);
            stream.Write(Encoding.ASCII.GetBytes(Convert.ToBase64String(key)));
        }
        catch (IOException) when (File.Exists(path))
        {
            // Another silo on this machine generated it first, and that is the key.
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    async Task<byte[]> GetKey()
    {
        await _lock.WaitAsync();
        try
        {
            return _key ??= await Resolve();
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task<byte[]?> GetPreviousKey()
    {
        await _lock.WaitAsync();
        try
        {
            if (!_previousLoaded)
            {
                var path = options.Value.Federation.Hosted.PreviousKeyEncryptionKeyPath;
                _previousKey = !string.IsNullOrEmpty(path) && File.Exists(path) ? ReadKeyFile(path) : null;
                _previousLoaded = true;
            }

            return _previousKey;
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task<byte[]> Resolve()
    {
        var path = options.Value.Federation.Hosted.KeyEncryptionKeyPath;
        if (!string.IsNullOrEmpty(path))
        {
            if (!File.Exists(path))
            {
                if (!environment.IsDevelopment())
                {
                    throw new FederationKeyEncryptionKeyUnavailable("the key-encryption key file does not exist. Provision it as a mounted secret");
                }

                Generate(path);
                logger.GeneratedKeyEncryptionKey(path);
            }

            return ReadKeyFile(path);
        }

        if (custody.KeepsInternalSecretsOutsideTheEventStoreDatabases)
        {
            var candidate = new EncryptionKey(RandomNumberGenerator.GetBytes(16), RandomNumberGenerator.GetBytes(KeySize));
            return (await custody.GetOrAddInternalSecretFor(EventStoreName.System, InternalSecretPurpose.FederationKeyProtection, candidate)).Private;
        }

        throw new FederationKeyEncryptionKeyUnavailable("Federation:Hosted:KeyEncryptionKeyPath is not set, and the storage backend keeps internal secrets in the databases it protects");
    }
}
