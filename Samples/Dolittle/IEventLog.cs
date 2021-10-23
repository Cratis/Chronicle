// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.SDK.Events;

namespace Sample
{
    public interface IEventLog
    {
        Task Append(EventSourceId eventSourceId, object @event);
    }
}
