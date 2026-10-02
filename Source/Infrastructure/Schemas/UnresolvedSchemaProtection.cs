// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when a schema cannot be resolved without risking unprotected values.
/// </summary>
/// <param name="reason">The unresolved schema declaration.</param>
public class UnresolvedSchemaProtection(string reason) : Exception($"Cannot resolve schema protection: {reason}");
