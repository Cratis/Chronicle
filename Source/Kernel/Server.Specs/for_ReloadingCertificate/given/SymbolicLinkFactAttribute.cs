// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.given;

/// <summary>
/// A fact that is reported as skipped on a platform where symbolic links cannot be created, such as Windows without the privilege.
/// Linux and macOS can always create them, so the fact runs there.
/// </summary>
public sealed class SymbolicLinkFactAttribute : FactAttribute
{
    static readonly Lazy<bool> _supported = new(Probe);

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolicLinkFactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The source file the fact is declared in, supplied by the compiler.</param>
    /// <param name="sourceLineNumber">The line the fact is declared on, supplied by the compiler.</param>
    public SymbolicLinkFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = Supported ? null : "This platform cannot create symbolic links.";
    }

    /// <summary>
    /// Gets a value indicating whether symbolic links can be created on this platform.
    /// </summary>
    public static bool Supported => _supported.Value;

    static bool Probe()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"chronicle-link-probe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.CreateSymbolicLink(Path.Combine(directory, "link"), "target");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
