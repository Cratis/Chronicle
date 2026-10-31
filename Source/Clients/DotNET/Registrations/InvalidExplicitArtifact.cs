// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception that is thrown when an explicitly nominated type cannot fulfill its artifact contract.
/// </summary>
/// <param name="artifactType">The nominated CLR type.</param>
/// <param name="kind">The artifact kind.</param>
/// <param name="reason">The invalid contract's reason.</param>
public class InvalidExplicitArtifact(Type artifactType, string kind, string reason)
    : Exception($"'{artifactType.FullName}' cannot be registered as '{kind}': {reason}");
