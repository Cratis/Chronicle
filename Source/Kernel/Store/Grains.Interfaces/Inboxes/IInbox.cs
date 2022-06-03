// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Inboxes;

/// <summary>
/// Defines an inbox for a microservices.
/// </summary>
public interface IInbox : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Start the inbox.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Start();
}
