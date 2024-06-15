// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the context of an event when being handled by a <see cref="IProjection"/>.
/// </summary>
/// <param name="Key"><see cref="Key"/> for the context.</param>
/// <param name="Event">The <see cref="AppendedEvent"/> that occurred.</param>
/// <param name="Changeset">The <see cref="IChangeset{Event, ExpandoObject}"/> to build on.</param>
public record ProjectionEventContext(Key Key, AppendedEvent Event, IChangeset<AppendedEvent, ExpandoObject> Changeset);
