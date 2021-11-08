// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents an entry being removed.
    /// </summary>
    /// <param name="Key"></param>
    public record RemovePerformed(object Key) : Change(new ExpandoObject());
}
