// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Properties;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.for_StoredTypes;

/// <summary>
/// The convention that camel cases element names covers only some namespaces. A type stored inside a document from
/// anywhere else gets its element names from the naming policy Arc holds in process-wide state - so it is written
/// differently by a host with another policy, and the same database is then read back differently. This walks everything
/// stored in a MongoDB document from the documents Chronicle persists and finds the types outside the covered namespaces.
/// </summary>
public class when_walking_the_shape_of_the_stored_types : Specification
{
    static readonly string[] _documents =
    [
        "Event", "UniqueConstraintIndex", "MongoDBSystemInformation", "MongoDBNamespace", "EventStore", "UniqueConstraintValueIndex",
        "ObserverState", "EventSequenceState", "ObserverPartitionCounts", "AlertIncidentDocument", "InFlightEvent", "FailedPartition",
        "EventSeeds", "ClosedStreamDocument", "WebhookSigningKey", "WebhookDefinition", "User", "Token", "StoredConstraintDefinition",
        "SingleWriterLeaseDocument", "SequenceQueryFolder", "SequenceQueryDefinition", "Scope", "ReplayedReadModel", "ReplayContext",
        "ReducerDefinition", "RecommendationState", "ReadModelChangeset", "ReadModel", "ReactorDefinition",
        "ProjectionDefinitionHistoryDocument", "Projection", "Patch", "ObserverDefinition", "MongoDBIdentity", "ExternalServiceDefinition",
        "EventType", "EventStoreSubscriptionDefinition", "EventSourceDefinition", "EventSequenceMutationHistoryEntry",
        "EventSequenceMutationHeadEntry", "EncryptionKeyErasureForIdentifier", "DataProtectionKey", "CaptureObservation", "Capture",
        "BehaviorPattern", "Authorization", "Application", "Administrator", "ProjectionDefinition"
    ];

    static readonly string[] _camelCasedNamespaces = ["Cratis.Chronicle.Storage", "Cratis.Chronicle.Concepts", "Cratis.Chronicle.Events"];
    static readonly string[] _storageAssemblies =
        ["Cratis.Chronicle.Concepts", "Cratis.Chronicle.Storage", "Cratis.Chronicle.Storage.MongoDB"];

    readonly HashSet<Type> _visited = [];
    List<Type> _outside;
    List<Type> _pinned;

    void Establish() => SpecSerializationSetup.Initialize();

    void Because()
    {
        _outside = [];
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => _storageAssemblies.Contains(_.GetName().Name))
            .SelectMany(LoadableTypes)
            .ToArray();
        var pins = typeof(PropertyPathClassMap).Assembly.GetTypes()
            .SelectMany(_ => _.GetInterfaces())
            .Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IBsonClassMapFor<>))
            .Select(_ => _.GetGenericArguments()[0])
            .ToHashSet();

        var candidates = types.Where(_ => !_.IsNested && _.Namespace?.StartsWith("Cratis.Chronicle", StringComparison.Ordinal) == true).ToArray();
        var queue = new Queue<Type>(_documents.SelectMany(name => DocumentsNamed(candidates, name)));
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!_visited.Add(type) || !BsonSerializer.LookupSerializer(type).GetType().Name.StartsWith("BsonClassMapSerializer"))
            {
                continue;
            }

            var classMap = BsonClassMap.LookupClassMap(type);
            if (!IsCamelCased(type))
            {
                _outside.Add(type);
            }

            foreach (var member in classMap.DeclaredMemberMaps)
            {
                foreach (var memberType in Unwrap(member.MemberType))
                {
                    queue.Enqueue(memberType);
                }
            }

            if (classMap.BaseClassMap is not null)
            {
                queue.Enqueue(classMap.BaseClassMap.ClassType);
            }
        }

        _pinned = [.. _outside.Where(pins.Contains)];
    }

    [Fact] void should_find_the_types_outside_the_camel_cased_namespaces() => _outside.ShouldNotBeEmpty();

    [Fact] void should_pin_every_one_of_them_with_a_class_map() => _outside.Except(_pinned).ShouldBeEmpty();

    /// <summary>
    /// Finds the types a document is stored as. The documents have the name of the kernel concept they hold, so the MongoDB
    /// representation is the one that is stored whenever there is one.
    /// </summary>
    /// <param name="candidates">The types to look through.</param>
    /// <param name="name">The name of the document.</param>
    /// <returns>The types stored under the name.</returns>
    static IEnumerable<Type> DocumentsNamed(Type[] candidates, string name)
    {
        var named = candidates.Where(_ => _.Name == name).ToArray();
        var inStorage = named.Where(_ => _.Assembly == typeof(PropertyPathClassMap).Assembly).ToArray();
        return inStorage.Length > 0 ? inStorage : named;
    }

    static bool IsCamelCased(Type type) => _camelCasedNamespaces.Any(_ => type.Namespace?.StartsWith(_, StringComparison.Ordinal) == true);

    static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    static IEnumerable<Type> Unwrap(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsArray)
        {
            return Unwrap(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments().SelectMany(Unwrap);
        }

        return type.IsPrimitive || type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true || type.Namespace?.StartsWith("MongoDB", StringComparison.Ordinal) == true
            ? []
            : [type];
    }
}
