// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Linq;
using Cratis.Arc.MongoDB;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.MongoDB.Integration;

/// <summary>
/// Lightweight base class for MongoDB integration specs that does not start an Orleans silo.
/// Provides BDD lifecycle (Establish → Because → Cleanup/Destroy) and MongoDB connectivity.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleInProcessFixture"/> providing the MongoDB container.</param>
public abstract class MongoDBSpecification(ChronicleInProcessFixture fixture) : IAsyncLifetime, IDisposable
{
    static MongoDBSpecification()
    {
        // Register the concept serialization provider so that ConceptAs<T> types (including
        // ConceptAs<Guid>) are serialized correctly without the full Chronicle startup.
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());

        // Register plain Guid and object serializers with Standard representation so that
        // BsonSerializer does not throw "GuidRepresentation is Unspecified".
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    /// <summary>
    /// Gets the MongoDB container.
    /// </summary>
    public IContainer MongoDBContainer => fixture.MongoDBContainer;

    /// <summary>
    /// Gets the docker network.
    /// </summary>
    public INetwork Network => fixture.Network;

    /// <summary>
    /// Gets the host port that MongoDB is listening on.
    /// </summary>
    protected int MongoDBPort => fixture.MongoDBContainer.GetMappedPublicPort(27017);

    /// <summary>
    /// Sets the name of the fixture. No-op for MongoDB-only specs.
    /// </summary>
    /// <param name="name">The name.</param>
    public virtual void SetName(string name) { }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await InvokeChain("Establish", baseFirst: true);
        await InvokeChain("Because", baseFirst: true);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await InvokeChain("Cleanup", baseFirst: false);
        await InvokeChain("Destroy", baseFirst: false);
    }

    /// <inheritdoc/>
    public virtual void Dispose() { }

    async Task InvokeChain(string methodName, bool baseFirst)
    {
        var methods = CollectMethods(methodName);

        if (!baseFirst)
        {
            methods.Reverse();
        }

        var results = methods.Select(method => method.Invoke(this, null));
        foreach (var result in results)
        {
            if (result is Task t)
            {
                await t;
            }
        }
    }

    List<MethodInfo> CollectMethods(string methodName)
    {
        var methods = new List<MethodInfo>();
        var type = GetType();

        while (type is not null && type != typeof(MongoDBSpecification))
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (method is not null)
            {
                methods.Insert(0, method);
            }

            type = type.BaseType;
        }

        return methods;
    }
}
