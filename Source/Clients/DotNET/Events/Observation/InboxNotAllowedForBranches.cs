// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

namespace Aksio.Cratis.Events.Observation;

/// <summary>
/// Exception that gets thrown when an observer for a branch type specifies inbox as source.
/// </summary>
public class InboxNotAllowedForBranches : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InboxNotAllowedForBranches"/> class.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> that is wrong.</param>
    public InboxNotAllowedForBranches(ObserverId observerId) : base($"Observer with identifier '{observerId}' can't be for inbox, branches only work with event log.")
    {
    }
}
