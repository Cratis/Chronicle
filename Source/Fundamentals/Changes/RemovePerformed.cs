// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Represents an entry being removed.
    /// </summary>
    /// <param name="Key">The key of the object that was removed.</param>
    public record RemovePerformed(object Key) : Change(null!);
}
