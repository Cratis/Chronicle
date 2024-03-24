// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.MongoDB;

/// <summary>
/// Represents the setup of MongoDB defaults.
/// </summary>
public static class MongoDBDefaults
{
    static readonly object _lock = new();
    static bool _initialized;

    /// <summary>
    /// Initializes the MongoDB defaults.
    /// </summary>
    /// <param name="mongoDBArtifacts">Optional <see cref="IMongoDBArtifacts"/> to use. Will default to <see cref="DefaultMongoDBArtifacts"/> which discovers at runtime.</param>
    /// <param name="jsonSerializerOptions">Optional The <see cref="JsonSerializerOptions"/> to use.</param>
    public static void Initialize(IMongoDBArtifacts? mongoDBArtifacts = default, JsonSerializerOptions? jsonSerializerOptions = default)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            mongoDBArtifacts ??= new DefaultMongoDBArtifacts(Types.Types.Instance);

            var conventionPackFilters = mongoDBArtifacts
                .ConventionPackFilters
                .Select(_ => (Activator.CreateInstance(_) as ICanFilterMongoDBConventionPacksForType)!)
                .ToArray();
            jsonSerializerOptions ??= Globals.JsonSerializerOptions;

            BsonSerializer
                .RegisterSerializationProvider(new ConceptSerializationProvider());
            BsonSerializer
                .RegisterSerializer(new DateTimeOffsetSupportingBsonDateTimeSerializer());
            BsonSerializer
                .RegisterSerializer(new DateOnlySerializer());
            BsonSerializer
                .RegisterSerializer(new TimeOnlySerializer());
            BsonSerializer
                .RegisterSerializer(new TypeSerializer());

#pragma warning disable CS0618

            // Due to what must be a bug in the latest MongoDB drivers, we set this explicitly as well.
            // This property is marked obsolete, we'll keep it here till that time
            // https://www.mongodb.com/community/forums/t/c-driver-2-11-1-allegedly-use-different-guid-representation-for-insert-and-for-find/8536/3
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
#pragma warning restore CS0618
            BsonSerializer
                .RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // When you have types with properties defined as object but could hold a Guid, the GuidRepresentation gets by default set to Unspecified.
            // By adding an object serializer for object configured explicitly with the Standard representation it should get serialized correctly and not throw an exception.
            // As described here: https://jira.mongodb.org/browse/CSHARP-3780
            BsonSerializer
                .RegisterSerializer(new ObjectSerializer(CustomObjectDiscriminatorConvention.Instance, GuidRepresentation.Standard, t => true));

            RegisterConventionAsPack(conventionPackFilters, AcronymFriendlyCamelCaseElementNameConvention.ConventionName, new AcronymFriendlyCamelCaseElementNameConvention());
            RegisterConventionAsPack(conventionPackFilters, ConventionPacks.IgnoreExtraElements, new IgnoreExtraElementsConvention(true));

            RegisterClassMaps(mongoDBArtifacts);
        }
    }

    static void RegisterClassMaps(IMongoDBArtifacts mongoDBArtifacts)
    {
        foreach (var classMapType in mongoDBArtifacts.ClassMaps)
        {
            var classMapProvider = Activator.CreateInstance(classMapType);
            var typeInterfaces = classMapType.GetInterfaces().Where(_ =>
            {
                var args = _.GetGenericArguments();
                if (args.Length == 1)
                {
                    return _ == typeof(IBsonClassMapFor<>).MakeGenericType(args[0]);
                }
                return false;
            });

            var method = typeof(MongoDBDefaults).GetMethod(nameof(Register), BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (var type in typeInterfaces)
            {
                var genericMethod = method.MakeGenericMethod(type.GenericTypeArguments[0]);
                genericMethod.Invoke(null, [classMapProvider]);
            }
        }
    }

    static void Register<T>(IBsonClassMapFor<T> classMapProvider)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }
        BsonClassMap.RegisterClassMap<T>(classMapProvider.Configure);
    }

    static void RegisterConventionAsPack(IEnumerable<ICanFilterMongoDBConventionPacksForType> conventionPackFilters, string name, IConvention convention)
    {
        var pack = new ConventionPack { convention };
        ConventionRegistry.Register(name, pack, type => ShouldInclude(conventionPackFilters, name, pack, type));
    }

    static bool ShouldInclude(IEnumerable<ICanFilterMongoDBConventionPacksForType> conventionPackFilters, string conventionPackName, IConventionPack conventionPack, Type type)
    {
        foreach (var filter in conventionPackFilters)
        {
            if (!filter.ShouldInclude(conventionPackName, conventionPack, type))
            {
                return false;
            }
        }

        return true;
    }
}
