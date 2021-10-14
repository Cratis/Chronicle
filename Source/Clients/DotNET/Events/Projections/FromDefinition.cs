// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the definition from for a specific event.
    /// </summary>
    public class FromDefinition : Dictionary<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FromDefinition"/> class.
        /// </summary>
        public FromDefinition() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FromDefinition"/> class.
        /// </summary>
        /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose values are copied into the <see cref="FromDefinition"/>.</param>
        public FromDefinition(IDictionary<string, string> dictionary) : base(dictionary) { }
    }
}
