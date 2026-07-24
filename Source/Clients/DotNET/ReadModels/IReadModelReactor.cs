// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Marker interface for a read model reactor — a type that reacts to read model instances being added,
/// modified or removed.
/// </summary>
/// <remarks>
/// Method dispatch is by convention. A method named <c>Added</c>, <c>Modified</c> or <c>Removed</c> is
/// invoked for the corresponding change. The first parameter is the read model — either a single instance
/// or an <see cref="IEnumerable{T}"/> of instances — and determines which read model is watched. Further
/// parameters are resolved as dependencies (the <see cref="Events.EventContext"/> of the causing event or
/// services from the service provider). Methods may be synchronous or asynchronous and may return events to
/// be appended as side effects, exactly like reactors.
/// </remarks>
public interface IReadModelReactor;
