// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents an operation in a <see cref="GraphChangeset"/>.
/// </summary>
/// <remarks>
/// The five operations are idempotent. A sink applies each one subject to the watermark rule: an operation that
/// carries an event sequence number at or below the watermark of the element it writes to is skipped. Property values
/// are primitives or arrays of primitives; a null value removes the property. Counter properties are resolved by the
/// engine against the current value, so an operation always carries the absolute value to store.
/// </remarks>
public abstract record GraphOperation;
