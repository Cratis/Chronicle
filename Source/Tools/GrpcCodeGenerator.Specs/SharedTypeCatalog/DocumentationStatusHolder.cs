// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SharedTypeCatalog;

/// <summary>
/// Holds a shared type discovered through a property rather than a service member.
/// </summary>
/// <param name="Status">The nested shared status.</param>
public record DocumentationStatusHolder(CoreOwnedStatus Status);
