// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.given;

public class a_catalog : Specification
{
    protected ExplicitArtifacts _options;
    protected IClientArtifactsProvider _discovered;
    protected IClientArtifactsProvider _catalog;
    protected EventStoreName _store;

    void Establish()
    {
        _options = new();
        _discovered = Substitute.For<IClientArtifactsProvider>();
        _store = "current";
    }

    protected static Type EmitType(string name, CustomAttributeBuilder[]? attributes = null, CustomAttributeBuilder[]? assemblyAttributes = null, Type[]? interfaces = null, Type? baseType = null)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        foreach (var attribute in assemblyAttributes ?? [])
        {
            assembly.SetCustomAttribute(attribute);
        }
        var type = assembly.DefineDynamicModule("Artifacts").DefineType(name, TypeAttributes.Public, baseType ?? typeof(object), interfaces);
        foreach (var attribute in attributes ?? [])
        {
            type.SetCustomAttribute(attribute);
        }

        return type.CreateType();
    }

    protected static CustomAttributeBuilder AttributeFor<T>(Type[] parameters, params object[] values)
        where T : Attribute => new(typeof(T).GetConstructor(parameters)!, values);

    private protected EventTypeDescriptor DescriptorFor(Type type) => ExplicitArtifactCatalog.From(_catalog)!.GetEventDescriptorFor(type, _store);

    public record PlainEvent;

    public record OtherEvent;

    public class PlainArtifact;
}
