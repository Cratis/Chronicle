// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// The exception that is thrown when a projection cannot use flat composite keys.
/// </summary>
/// <param name="reason">The reason the definition is not supported.</param>
public class FlatCompositeKeyNotSupported(string reason) : Exception(reason);
