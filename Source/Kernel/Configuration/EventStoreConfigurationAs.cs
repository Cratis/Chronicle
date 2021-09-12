// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Configuration
{
    /// <summary>
    /// Delegate for providing a method for getting event store configuration specifically serialized for a given type.
    /// </summary>
    /// <typeparam name="T">Type to get.</typeparam>
    /// <returns>Instance in the type asked for.</returns>
    public delegate T EventStoreConfigurationAs<T>();
}