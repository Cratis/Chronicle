// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Exception that gets thrown when an expected property on an event does not exist on a model during convention based mapping.
    /// </summary>
    public class MissingExpectedEventPropertyOnModel : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingExpectedEventPropertyOnModel"/> class.
        /// </summary>
        /// <param name="eventType">Type of event.</param>
        /// <param name="modelType">Type of model.</param>
        /// <param name="property">Name of property.</param>
        public MissingExpectedEventPropertyOnModel(Type eventType, Type modelType, string property)
            : base($"Expected property '{property}' on event '{eventType.FullName}' does not exist on '{modelType.FullName}'")
        {
        }
    }
}
