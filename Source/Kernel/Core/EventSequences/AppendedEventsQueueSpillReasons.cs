// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The reasons an observer is spilled from live delivery to catch-up, used as the <c language="csharp">Reason</c> tag of the spill counter.
/// </summary>
internal static class AppendedEventsQueueSpillReasons
{
    /// <summary>The queue was full.</summary>
    internal const string QueueFull = "QueueFull";

    /// <summary>Dispatching to the observer failed.</summary>
    internal const string DispatchFailed = "DispatchFailed";

    /// <summary>The observer's share of one dispatch exceeded the partition run limit.</summary>
    internal const string FragmentedBacklog = "FragmentedBacklog";

    /// <summary>The observer's own backlog in the queue exceeded the queue capacity.</summary>
    internal const string LaggingBacklog = "LaggingBacklog";

    /// <summary>The events were committed without live delivery, as an import commits its chunks (decision 0023).</summary>
    internal const string CommittedOutsideLiveDelivery = "CommittedOutsideLiveDelivery";
}
