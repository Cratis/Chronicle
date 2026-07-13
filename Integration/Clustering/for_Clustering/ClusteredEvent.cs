// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[EventType]
public record ClusteredEvent(int Number, ThingId Reference, Priority Priority, IList<string> Tags, Address Location);
