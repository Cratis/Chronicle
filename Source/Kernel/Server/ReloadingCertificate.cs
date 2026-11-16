// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
namespace Cratis.Chronicle.Server;

/// <summary>
/// Holds the TLS certificate the Chronicle listeners present and swaps it when the configured file changes on disk,
/// so a renewed certificate (for instance from cert-manager, mounted from a Kubernetes Secret) is picked up without a restart.
/// </summary>
/// <remarks>
/// The file is checked on a TLS handshake, at most once per check interval, rather than with a file system watcher,
/// because watchers do not reliably see the atomic symlink swap Kubernetes uses to update mounted Secrets.
/// A file that cannot be read or parsed (for instance half-written) leaves the current certificate in place.
/// Replaced certificates are not disposed, because handshakes already in flight may still hold them.
/// </remarks>
internal sealed class ReloadingCertificate
{
    readonly Tls _tls;
    readonly TimeSpan _checkInterval;
    readonly TimeProvider _timeProvider;
    readonly Action<X509Certificate2>? _reloaded;
    readonly Action<Exception>? _failed;
    readonly object _lock = new();
    volatile X509Certificate2 _current;
    (DateTime Written, long Length) _loadedFrom;
    long _nextCheck;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadingCertificate"/> class.
    /// </summary>
    /// <param name="tls">The <see cref="Tls"/> configuration the certificate came from.</param>
    /// <param name="initial">The certificate loaded at startup from <see cref="Tls.CertificatePath"/>.</param>
    /// <param name="checkInterval">The shortest time between two checks of the file.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
    /// <param name="reloaded">Called with the new certificate after a reload.</param>
    /// <param name="failed">Called with the error when a changed file could not be loaded.</param>
    internal ReloadingCertificate(
        Tls tls,
        X509Certificate2 initial,
        TimeSpan checkInterval,
        TimeProvider timeProvider,
        Action<X509Certificate2>? reloaded = null,
        Action<Exception>? failed = null)
    {
        _tls = tls;
        _current = initial;
        _checkInterval = checkInterval;
        _timeProvider = timeProvider;
        _reloaded = reloaded;
        _failed = failed;
        _loadedFrom = Stamp();
        _nextCheck = _timeProvider.GetTimestamp() + ToTicks(checkInterval);
    }

    /// <summary>
    /// Gets the certificate to present, reloading it first if the file changed since it was last loaded.
    /// </summary>
    public X509Certificate2 Current
    {
        get
        {
            var now = _timeProvider.GetTimestamp();
            if (now >= Interlocked.Read(ref _nextCheck))
            {
                Reload(now);
            }
            return _current;
        }
    }

    long ToTicks(TimeSpan span) => (long)(span.TotalSeconds * _timeProvider.TimestampFrequency);

    (DateTime Written, long Length) Stamp()
    {
        try
        {
            // FileInfo follows symlinks, which is what a Kubernetes Secret volume uses.
            var info = new FileInfo(_tls.CertificatePath!);
            return info.Exists ? (info.LastWriteTimeUtc, info.Length) : default;
        }
        catch (IOException)
        {
            return default;
        }
    }

    void Reload(long now)
    {
        if (!Monitor.TryEnter(_lock))
        {
            return;
        }

        try
        {
            Interlocked.Exchange(ref _nextCheck, now + ToTicks(_checkInterval));
            var stamp = Stamp();
            if (stamp == default || stamp == _loadedFrom)
            {
                return;
            }

            try
            {
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(_tls.CertificatePath!, _tls.CertificatePassword);
                _current = certificate;
                _loadedFrom = stamp;
                _reloaded?.Invoke(certificate);
            }
            catch (Exception ex) when (ex is IOException or System.Security.Cryptography.CryptographicException or UnauthorizedAccessException)
            {
                // Remember the stamp so a broken file is reported once, not on every check; the next change is tried again.
                _loadedFrom = stamp;
                _failed?.Invoke(ex);
            }
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
}
