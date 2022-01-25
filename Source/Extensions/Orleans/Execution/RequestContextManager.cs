// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans.Runtime;

namespace Aksio.Cratis.Extensions.Orleans.Execution
{
    /// <summary>
    /// Represents an implementation of <see cref="IRequestContextManager"/>.
    /// </summary>
    [Singleton]
    public class RequestContextManager : IRequestContextManager
    {
        /// <inheritdoc/>
        public object Get(string key) => RequestContext.Get(key);

        /// <inheritdoc/>
        public void Set(string key, object value) => RequestContext.Set(key, value);
    }
}
