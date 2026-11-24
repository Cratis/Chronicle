// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

public partial class EventTypesStorage
{
    static EventTypeSchema LatestFor(EventType document) => document.ToKernelForLatestGeneration();
}
