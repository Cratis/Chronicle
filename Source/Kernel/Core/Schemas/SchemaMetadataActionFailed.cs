// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when applying or releasing schema metadata for a property fails.
/// </summary>
/// <remarks>
/// A failure to release is almost always an identifier mismatch, and the underlying cryptography reports it only
/// as an opaque padding error. Every value marked with a given schema metadata type is encrypted and released
/// under one identifier - a subject for <c language="csharp">[PII]</c>, or a subject, a namespace, or a fixed global
/// identifier for <c language="csharp">[Encrypted]</c>, depending on its configured scope - so a
/// value encrypted under a different identifier cannot be read back. For <c language="csharp">[PII]</c> the usual cause
/// is a projection join that copies a value out of another event source's stream.
/// </remarks>
/// <param name="action">The action that failed — <c language="csharp">apply</c> or <c language="csharp">release</c>.</param>
/// <param name="propertyPath">The path of the property being handled.</param>
/// <param name="identifier">The identifier the value was handled under.</param>
/// <param name="error">The underlying error.</param>
public class SchemaMetadataActionFailed(string action, string propertyPath, string identifier, Exception error)
    : Exception(BuildMessage(action, propertyPath, identifier, error), error)
{
    /// <summary>
    /// The action name used when releasing schema metadata.
    /// </summary>
    public const string ReleaseAction = "release";

    /// <summary>
    /// The action name used when applying schema metadata.
    /// </summary>
    public const string ApplyAction = "apply";

    static string BuildMessage(string action, string propertyPath, string identifier, Exception error) =>
        $"Failed to {action} schema metadata for property '{propertyPath}' of '{identifier}'.{IdentifierMismatchHint(action, identifier, error)}";

    static string IdentifierMismatchHint(string action, string identifier, Exception error) =>
        action == ReleaseAction && IsCryptographic(error)
            ? $" The stored value could not be decrypted with the encryption key for '{identifier}', so it was encrypted under a different identifier or its ownership metadata is missing."
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
