// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Boot
{
    /// <summary>
    /// Marker interface that can be used for a boot procedure that will be called automatically during startup.
    /// </summary>
    public interface IPerformBootProcedure
    {
        /// <summary>
        /// The method that will be called.
        /// </summary>
        void Perform();
    }
}
