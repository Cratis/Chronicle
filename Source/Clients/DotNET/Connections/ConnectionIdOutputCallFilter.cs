// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Orleans.Execution;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Cratis.Connections
{
    /// <summary>
    /// Represents a <see cref="IOutgoingGrainCallFilter"/> for adding <see cref="ConnectionId"/> to the context.
    /// </summary>
    public class ConnectionIdOutputCallFilter : IOutgoingGrainCallFilter
    {
        readonly IServiceProvider _serviceProvider;
        bool _initialized;
        IRequestContextManager? _requestContextManager;
        IConnectionManager? _connectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionIdOutputCallFilter"/> class.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
        public ConnectionIdOutputCallFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            if (!_initialized)
            {
                _initialized = true;
                _connectionManager = _serviceProvider.GetService<IConnectionManager>();
                _requestContextManager = _serviceProvider.GetService<IRequestContextManager>();
            }

            _requestContextManager!.Set(
                    RequestContextKeys.ConnectionId,
                    _connectionManager!.CurrentConnectionId.Value);
            await context.Invoke();
        }
    }
}
