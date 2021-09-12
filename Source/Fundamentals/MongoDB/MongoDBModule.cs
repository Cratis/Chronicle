// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.MongoDB
{
    /// <summary>
    /// Represents a <see cref="Module"/> for setting up defaults and bindings for MongoDB
    /// </summary>
    public class MongoDBModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            BsonSerializer
                .RegisterSerializationProvider(
                    new ConceptSerializationProvider()
                );
            BsonSerializer
                .RegisterSerializer(
                    new DateTimeOffsetSupportingBsonDateTimeSerializer()
                );

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
