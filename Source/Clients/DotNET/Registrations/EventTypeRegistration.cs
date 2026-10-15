// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The immutable supplied facets captured from one explicit event declaration.
/// </summary>
/// <param name="Type">The nominated CLR type.</param>
/// <param name="Id">The supplied identity.</param>
/// <param name="Generation">The supplied generation.</param>
/// <param name="Visibility">The supplied registration visibility.</param>
/// <param name="EventStore">The supplied owning store.</param>
/// <param name="Sequence">The supplied publishing sequence.</param>
internal sealed record EventTypeRegistration(
    Type Type,
    DeclaredFacet<EventTypeId>? Id = null,
    DeclaredFacet<EventTypeGeneration>? Generation = null,
    DeclaredFacet<EventTypeVisibility>? Visibility = null,
    DeclaredFacet<EventStoreName>? EventStore = null,
    DeclaredFacet<EventSequenceId>? Sequence = null);
