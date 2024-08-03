// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when no event types have been added to a unique constraint.
/// </summary>
public class MissingNameForUniqueConstraint() : Exception("Missing name for unique constraint");
