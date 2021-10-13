// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an observable for reacting when the projections system is ready to go.
    /// </summary>
    public class ProjectionsReady
    {
        /// <summary>
        /// Gets the observable for seeing if the projection system is ready.
        /// </summary>
        public ReplaySubject<bool>    IsReady { get; } = new ();
    }
}
