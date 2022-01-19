// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Boot
{
    /// <summary>
    /// Represents an implementation of <see cref="IBootProcedures"/>.
    /// </summary>
    public class BootProcedures : IBootProcedures
    {
        readonly IInstancesOf<IPerformBootProcedure> _procedures;

        /// <summary>
        /// Initializes a new instance of <see cref="BootProcedures"/>.
        /// </summary>
        /// <param name="procedures"><see cref="IInstancesOf{T}"/> <see cref="IPerformBootProcedure"/>.</param>
        public BootProcedures(IInstancesOf<IPerformBootProcedure> procedures) => _procedures = procedures;

        /// <inheritdoc/>
        public void Perform() => _procedures.ForEach(_ => _.Perform());
    }
}
