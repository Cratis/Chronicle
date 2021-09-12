// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Cratis.Execution
{
    /// <summary>
    /// Defines a wrapper manager for the Orleans <see cref="RequestContext"/>.
    /// </summary>
    public interface IRequestContextManager
    {
        /// <summary>
        /// Retrieve a value from the RequestContext key-value bag.
        /// </summary>
        /// <param name="key">The key for the value to be retrieved.</param>
        /// <returns>
        /// The value currently in the RequestContext for the specified key, otherwise returns
        /// null if no data is present for that key.
        /// </returns>
        object Get(string key);

        /// <summary>
        /// Sets a value into the RequestContext key-value bag.
        /// </summary>
        /// <param name="key">The key for the value to be updated / added.</param>
        /// <param name="value">The value to be stored into RequestContext.</param>
        void Set(string key, object value);
    }
}
