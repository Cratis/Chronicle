// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Grpc
{
    /// <summary>
    /// Defines a wrapper interface for working with underlying <see cref="GrpcChannel">GRPC channels</see>.
    /// </summary>
    public interface IGrpcChannel : IDisposable
    {
        /// <summary>
        /// Creates a code-first service backed by a <see cref="ChannelBase"/> instance.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        TService CreateGrpcService<TService>() where TService : class;
    }
}
