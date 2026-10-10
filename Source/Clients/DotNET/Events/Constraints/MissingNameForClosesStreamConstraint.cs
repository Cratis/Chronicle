// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when a closing declaration attempts to use the reserved manual owner name.
/// </summary>
public class MissingNameForClosesStreamConstraint() : Exception("A closing constraint must have a non-empty owner name; the empty name is reserved for manual closures.");
