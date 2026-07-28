// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// The exception that is thrown when applying or releasing compliance metadata for a property fails.
/// </summary>
/// <remarks>
/// A failure to release is almost always a subject mismatch, and the underlying cryptography reports it only as an
/// opaque padding error. Chronicle keeps one compliance subject per read model and releases all of its PII under
/// that subject, so a value encrypted under a different subject cannot be read back — the usual cause being a
/// projection join that copies a <c>[PII]</c> value out of another event source's stream.
/// </remarks>
/// <param name="action">The action that failed — <c>apply</c> or <c>release</c>.</param>
/// <param name="propertyPath">The path of the property being handled.</param>
/// <param name="identifier">The compliance subject the value was handled under.</param>
/// <param name="error">The underlying error.</param>
public class ComplianceMetadataActionFailed(string action, string propertyPath, string identifier, Exception error)
    : Exception(BuildMessage(action, propertyPath, identifier, error), error)
{
    /// <summary>
    /// The action name used when releasing compliance metadata.
    /// </summary>
    public const string ReleaseAction = "release";

    /// <summary>
    /// The action name used when applying compliance metadata.
    /// </summary>
    public const string ApplyAction = "apply";

    static string BuildMessage(string action, string propertyPath, string identifier, Exception error) =>
        $"Failed to {action} compliance metadata for property '{propertyPath}' of '{identifier}'.{SubjectMismatchHint(action, identifier, error)}";

    static string SubjectMismatchHint(string action, string identifier, Exception error) =>
        action == ReleaseAction && IsCryptographic(error)
            ? $" The stored value could not be decrypted with the encryption key for subject '{identifier}', so it was encrypted under a different subject. A read model has one compliance subject and all of its [PII] is released under it, so a value belonging to another subject cannot be read back. The usual cause is a projection join copying a [PII] value out of another event source's stream — see CHR0038."
            : string.Empty;

    static bool IsCryptographic(Exception? error)
    {
        for (var current = error; current is not null; current = current.InnerException)
        {
            if (current is CryptographicException)
            {
                return true;
            }
        }

        return false;
    }
}
