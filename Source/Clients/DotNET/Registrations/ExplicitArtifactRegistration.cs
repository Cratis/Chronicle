// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// An immutable type nomination and its optional supplied identity.
/// </summary>
/// <param name="Type">The nominated CLR type.</param>
/// <param name="Kind">The artifact kind.</param>
/// <param name="Identity">The optional supplied identity.</param>
internal sealed record ExplicitArtifactRegistration(Type Type, ExplicitArtifactKind Kind, DeclaredFacet<string>? Identity);
