// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// Something that records the sequence numbers of the events its observer has handled.
/// </summary>
public interface IUnseenObserver
{
    /// <summary>
    /// Gets the sequence numbers handled, in the order they were handled.
    /// </summary>
    ConcurrentQueue<EventSequenceNumber> Handled { get; }
}
