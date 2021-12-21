// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Cratis.Events;
using Cratis.Events.Observation;
using Cratis.Grpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Client;

namespace Cratis.Hosting
{
    /// <summary>
    /// Represents an implementation of <see cref="IClientBuilder"/>.
    /// </summary>
    public class ClientBuilder : IClientBuilder
    {
        readonly MicroserviceId _microserviceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientBuilder"/> class.
        /// </summary>
        /// <param name="microserviceId">Microservice identifier.</param>
        public ClientBuilder(MicroserviceId microserviceId)
        {
            _microserviceId = microserviceId;
        }

        public static IClientBuilder ForMicroservice(MicroserviceId id)
        {
            return new ClientBuilder(id);
        }

        public void Build(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            var channel = new GrpcChannel("http://localhost:5002");
            var types = new Types.Types();

            services.AddSingleton<Types.ITypes>(types);
            services.AddSingleton<IGrpcChannel>(channel);
            services.AddSingleton<IEventStore, EventStore>();
            services.AddSingleton<IObservers, Observers>();
            services.AddSingleton<IEventTypes, EventTypes>();
            services.AddSingleton<IEventSerializer, EventSerializer>();
            services.AddSingleton<IHostedService, ObserversService>();

            types.AllObservers().ForEach(_ => services.AddTransient(_));
        }
    }
}
