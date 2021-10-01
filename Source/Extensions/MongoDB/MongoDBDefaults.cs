// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents the setup of MongoDB defaults.
    /// </summary>
    public static class MongoDBDefaults
    {
        static bool _initialized;

        /// <summary>
        /// Initialize the defaults.
        /// </summary>
        public static void Initialize()
        {
            if( _initialized ) return;
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

            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new CamelCaseElementNameConvention()
            };
            ConventionRegistry.Register("Cratis Conventions", conventionPack, _ => true);
        }
    }
}
