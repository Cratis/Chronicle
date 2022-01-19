// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents the setup of MongoDB defaults.
    /// </summary>
    [Singleton]
    public class MongoDBDefaults
    {
        static bool _initialized;
        readonly IInstancesOf<ICanFilterMongoDBConventionPacksForType> _conventionPackFilters;
        readonly ITypes _types;
        readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBDefaults"/> class.
        /// </summary>
        /// <param name="conventionPackFilters"><see cref="IInstancesOf{T}"/> <see cref="ICanFilterMongoDBConventionPacksForType"/>.</param>
        /// <param name="types"><see cref="ITypes"/> for general type discovery.</param>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing instances of types.</param>
        public MongoDBDefaults(IInstancesOf<ICanFilterMongoDBConventionPacksForType> conventionPackFilters, ITypes types, IServiceProvider serviceProvider)
        {
            _conventionPackFilters = conventionPackFilters;
            _types = types;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Initialize the defaults.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            BsonSerializer
                .RegisterSerializationProvider(new ConceptSerializationProvider());
            BsonSerializer
                .RegisterSerializer(new DateTimeOffsetSupportingBsonDateTimeSerializer());

#pragma warning disable CS0618

            // Due to what must be a bug in the latest MongoDB drivers, we set this explicitly as well.
            // This property is marked obsolete, we'll keep it here till that time
            // https://www.mongodb.com/community/forums/t/c-driver-2-11-1-allegedly-use-different-guid-representation-for-insert-and-for-find/8536/3
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
#pragma warning restore CS0618
            BsonSerializer
                .RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            RegisterConventionAsPack(ConventionPacks.CamelCase, new CamelCaseElementNameConvention());
            RegisterConventionAsPack(ConventionPacks.IgnoreExtraElements, new IgnoreExtraElementsConvention(true));

            RegisterClassMaps();
        }

        void RegisterClassMaps()
        {
            foreach (var classMapType in _types.FindMultiple(typeof(IBsonClassMapFor<>)))
            {
                var classMapProvider = _serviceProvider.GetService(classMapType);
                var typeInterfaces = classMapType.GetInterfaces().Where(_ =>
                {
                    var args = _.GetGenericArguments();
                    if (args.Length == 1)
                    {
                        return _ == typeof(IBsonClassMapFor<>).MakeGenericType(args[0]);
                    }
                    return false;
                });

                var method = typeof(MongoDBDefaults).GetMethod(nameof(Register), BindingFlags.Instance | BindingFlags.NonPublic)!;
                foreach (var type in typeInterfaces)
                {
                    var genericMethod = method.MakeGenericMethod(type.GenericTypeArguments[0]);
                    genericMethod.Invoke(this, new[] { classMapProvider });
                }
            }
        }

        void Register<T>(IBsonClassMapFor<T> classMapProvider)
        {
            BsonClassMap.RegisterClassMap<T>(_ => classMapProvider.Configure(_));
        }

        void RegisterConventionAsPack(string name, IConvention convention)
        {
            var pack = new ConventionPack { convention };
            ConventionRegistry.Register(name, pack, type => ShouldInclude(name, pack, type));
        }

        bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type)
        {
            foreach (var filter in _conventionPackFilters)
            {
                if (!filter.ShouldInclude(conventionPackName, conventionPack, type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
