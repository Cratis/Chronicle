// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Hosting
{
    /// <summary>
    /// Defines a builder for building client ready for use.
    /// </summary>
    public interface IClientBuilder
    {
        /// <summary>
        /// Instruct the builder that the client is within a silo.
        /// </summary>
        /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
        IClientBuilder InSilo();

        /// <summary>
        /// Build the client.
        /// </summary>
        /// <param name="hostBuilderContext"><see cref="HostBuilderContext"/> we're building for.</param>
        /// <param name="services"><see cref="IServiceCollection"/> to register services for.</param>
        /// <param name="types">Optional <see cref="ITypes"/> for type discovery.</param>
        void Build(HostBuilderContext hostBuilderContext, IServiceCollection services, ITypes? types = default);
    }
}
