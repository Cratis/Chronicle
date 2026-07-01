// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child entry for an assigned contact, keyed by a <see cref="ContactId"/> concept.
/// </summary>
/// <param name="ContactId">The contact identifier, used as the child key.</param>
/// <param name="Name">The contact name.</param>
public record ContactEntry(
    ContactId ContactId,
    string Name);
