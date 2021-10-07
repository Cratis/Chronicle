// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Delegate that represents getting a value from a <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="input">Input <see cref="ExpandoObject"/>.</param>
    /// <returns>Value.</returns>
    public delegate object PropertyAccessor(ExpandoObject input);
}
