// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Boot
{
    /// <summary>
    /// Defines a system that can deal with <see cref="IPerformBootProcedure"/>.
    /// </summary>
    public interface IBootProcedures
    {
        /// <summary>
        /// Perform the boot procedures.
        /// </summary>
        void Perform();
    }
}
