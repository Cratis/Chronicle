// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a projection declared as a variant does not name the event that activates
/// it.
/// </summary>
/// <remarks>
/// A variant with no entering event could never be created, and - because every other handler on a variant is
/// update-only - would silently never be written to at all.
/// </remarks>
/// <param name="declaringType">The variant that is missing its entering event.</param>
public class VariantMustDeclareEntersOnEvent(Type declaringType)
    : Exception($"Variant '{declaringType.FullName}' must declare at least one EntersOn event to specify what activates it.");
