// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Client;

namespace Cratis.Grpc
{
    /// <summary>
    /// Represents an implementation of <see cref="IGrpcChannel"/>.
    /// </summary>
    public class GrpcChannel : IGrpcChannel
    {
        readonly global::Grpc.Net.Client.GrpcChannel _underlyingChannel;

        public GrpcChannel(string address)
        {
            _underlyingChannel = global::Grpc.Net.Client.GrpcChannel.ForAddress(address);
        }

        /// <inheritdoc/>
        public TService CreateGrpcService<TService>() where TService : class => _underlyingChannel.CreateGrpcService<TService>();

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _underlyingChannel.Dispose();
        }
    }
}
