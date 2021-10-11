// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents the setup of MongoDB defaults.
    /// </summary>
    [Singleton]
    public class MongoDBDefaults
    {
        readonly IInstancesOf<ICanFilterMongoDBConventionPacksForType> _conventionPackFilters;
        bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBDefaults"/> class.
        /// </summary>
        /// <param name="conventionPackFilters"><see cref="IInstancesOf{T}"/> <see cref="ICanFilterMongoDBConventionPacksForType"/>.</param>
        public MongoDBDefaults(IInstancesOf<ICanFilterMongoDBConventionPacksForType> conventionPackFilters)
        {
            _conventionPackFilters = conventionPackFilters;
        }

        /// <summary>
        /// Initialize the defaults.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            BsonSerializer
                .RegisterSerializationProvider(
                    new ConceptSerializationProvider()
                );
            BsonSerializer
                .RegisterSerializer(
                    new DateTimeOffsetSupportingBsonDateTimeSerializer()
                );

#pragma warning disable CS0618
            // Due to what must be a bug in the latest MongoDB drivers, we set this explicitly as well.
            // This property is marked obsolete, we'll keep it here till that time
            // https://www.mongodb.com/community/forums/t/c-driver-2-11-1-allegedly-use-different-guid-representation-for-insert-and-for-find/8536/3
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
#pragma warning restore CS0618
            BsonSerializer
                .RegisterSerializer(
                    new GuidSerializer(GuidRepresentation.Standard)
                );

            RegisterConventionAsPack(ConventionPacks.CamelCase, new CamelCaseElementNameConvention());
            RegisterConventionAsPack(ConventionPacks.IgnoreExtraElements, new IgnoreExtraElementsConvention(true));
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
