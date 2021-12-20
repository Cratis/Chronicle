// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;

#pragma warning disable CS8019  // TODO: Orleans CodeGenerator is failing due to this: https://www.ingebrigtsen.info/2021/08/13/orleans-and-c-10-global-usings/
using System.Threading.Tasks;

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Defines an observer of events in an event log.
    /// </summary>
    public interface IObserver
    {
        Task Next(AppendedEvent @event);
    }
}
