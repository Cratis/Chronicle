// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A temporary directory to hold key-encryption key files in, and a way to build the <see cref="FederationKeyProtection"/> that reads them.
/// </summary>
public sealed class KeyEncryptionKeyFile : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"chronicle-kek-{Guid.NewGuid():N}");

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyEncryptionKeyFile"/> class.
    /// </summary>
    public KeyEncryptionKeyFile() => Directory.CreateDirectory(_directory);

    /// <summary>
    /// Gets a path in the directory that does not exist yet.
    /// </summary>
    public string Missing => Path.Combine(_directory, $"{Guid.NewGuid():N}.key");

    /// <summary>
    /// Create a file holding a fresh key.
    /// </summary>
    /// <returns>The path of the file.</returns>
    public string Create()
    {
        var path = Missing;
        File.WriteAllText(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        return path;
    }

    /// <summary>
    /// Build a key protection.
    /// </summary>
    /// <param name="path">The configured key-encryption key path, if any.</param>
    /// <param name="development">Whether the environment is Development.</param>
    /// <param name="custody">The key custody, or a substitute that holds internal secrets in the database when not given.</param>
    /// <param name="previousPath">The configured previous key-encryption key path, if any.</param>
    /// <returns>The <see cref="FederationKeyProtection"/>.</returns>
    public static FederationKeyProtection Protection(string? path, bool development = false, IEncryptionKeyStorage? custody = null, string? previousPath = null)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(development ? Environments.Development : Environments.Production);
        var options = Options.Create(new ChronicleOptions
        {
            Federation = new() { Hosted = new() { Enabled = true, KeyEncryptionKeyPath = path, PreviousKeyEncryptionKeyPath = previousPath } }
        });
        return new(options, environment, custody ?? Substitute.For<IEncryptionKeyStorage>(), NullLogger<FederationKeyProtection>.Instance);
    }

    /// <inheritdoc/>
    public void Dispose() => Directory.Delete(_directory, true);
}
