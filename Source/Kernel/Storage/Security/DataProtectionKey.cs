// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents a stored Data Protection key.
/// </summary>
/// <param name="Id">The unique identifier for the key.</param>
/// <param name="FriendlyName">The friendly name of the key.</param>
/// <param name="Xml">The XML representation of the key.</param>
/// <param name="Created">When the key was created.</param>
public record DataProtectionKey(string Id, string FriendlyName, string Xml, DateTimeOffset Created);
