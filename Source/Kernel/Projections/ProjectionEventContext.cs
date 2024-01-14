// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Projections;

/// <summary>
/// Represents the context of an event when being handled by a <see cref="IProjection"/>.
/// </summary>
/// <param name="Key"><see cref="Key"/> for the context.</param>
/// <param name="Event">The <see cref="AppendedEvent"/> that occurred.</param>
/// <param name="Changeset">The <see cref="IChangeset{Event, ExpandoObject}"/> to build on.</param>
public record ProjectionEventContext(Key Key, AppendedEvent Event, IChangeset<AppendedEvent, ExpandoObject> Changeset);
