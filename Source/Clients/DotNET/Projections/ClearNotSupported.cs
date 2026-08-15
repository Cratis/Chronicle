// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a read model properties builder implementation does not support clearing a member.
/// </summary>
/// <param name="builderType">The type of builder that does not support clearing a member.</param>
public class ClearNotSupported(Type builderType)
    : Exception($"The read model properties builder implementation '{builderType.FullName}' does not support clearing a member.");
