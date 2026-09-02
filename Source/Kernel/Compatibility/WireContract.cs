// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents everything one version of Chronicle puts on the wire.
/// </summary>
/// <param name="Services">The services, keyed by fully qualified name.</param>
/// <param name="Messages">The messages, keyed by fully qualified name.</param>
/// <param name="Enums">The enums, keyed by fully qualified name.</param>
/// <remarks>
/// This is the normalized form both sides of every comparison are reduced to. It is deliberately smaller than a
/// <c>FileDescriptorSet</c>: it drops which file something was declared in, comments, and options - none of which
/// reach the wire - so that moving a message between proto files does not read as a breaking change.
/// </remarks>
public record WireContract(
    IReadOnlyDictionary<string, WireService> Services,
    IReadOnlyDictionary<string, WireMessage> Messages,
    IReadOnlyDictionary<string, WireEnum> Enums);
