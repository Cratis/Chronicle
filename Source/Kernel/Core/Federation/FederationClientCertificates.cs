// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Loads named PKCS#12 client certificates from configuration, with the same rules as the federation listener: the password is read
/// from a file, never from configuration itself. Certificates are cached and reloaded when the file changes; replaced certificates
/// stay alive (a connection may still use them) until this provider is disposed.
/// </summary>
/// <param name="options">The Chronicle options.</param>
[Singleton]
public sealed class FederationClientCertificates(IOptions<ChronicleOptions> options) : IFederationClientCertificates
{
    readonly Lock _lock = new();
    readonly Dictionary<string, Loaded> _loaded = new(StringComparer.Ordinal);
    readonly List<X509Certificate2> _retired = [];
    bool _disposed;

    /// <inheritdoc/>
    public X509Certificate2 Get(string name)
    {
        if (!options.Value.Federation.Certificates.TryGetValue(name, out var configuration) || string.IsNullOrEmpty(configuration.Path))
        {
            throw new InvalidOperationException($"No client certificate named '{name}' is configured in Federation:Certificates.");
        }

        if (!File.Exists(configuration.Path))
        {
            throw new InvalidOperationException($"The file of the client certificate named '{name}' was not found.");
        }

        var written = File.GetLastWriteTimeUtc(configuration.Path);
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_loaded.TryGetValue(name, out var existing) && existing.Path == configuration.Path && existing.Written == written)
            {
                return existing.Certificate;
            }

            var password = !string.IsNullOrEmpty(configuration.PasswordPath) && File.Exists(configuration.PasswordPath)
                ? File.ReadAllText(configuration.PasswordPath).TrimEnd('\r', '\n')
                : null;

            var certificate = X509CertificateLoader.LoadPkcs12FromFile(configuration.Path, password);
            if (existing is not null)
            {
                _retired.Add(existing.Certificate);
            }

            _loaded[name] = new Loaded(configuration.Path, written, certificate);
            return certificate;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var loaded in _loaded.Values)
            {
                loaded.Certificate.Dispose();
            }

            foreach (var certificate in _retired)
            {
                certificate.Dispose();
            }

            _loaded.Clear();
            _retired.Clear();
        }
    }

    sealed record Loaded(string Path, DateTime Written, X509Certificate2 Certificate);
}
