// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;

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
