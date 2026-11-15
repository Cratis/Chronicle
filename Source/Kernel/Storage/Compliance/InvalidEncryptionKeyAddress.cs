// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when a component of an encryption key address cannot be turned into a storage location.
/// </summary>
/// <remarks>
/// The message names the kind of component and never its value, because identifiers are subject data.
/// </remarks>
/// <param name="component">The kind of component that was rejected, for example <c language="csharp">identifier</c>.</param>
public class InvalidEncryptionKeyAddress(string component)
    : EncryptionKeyIdentifierRefused($"The {component} of an encryption key address is empty, too long or not valid text.");
