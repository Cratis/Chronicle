// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Google.Protobuf.Reflection;

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Reads the canonical descriptor set that every Chronicle contracts package ships into a <see cref="WireContract"/>.
/// </summary>
public static class WireContractReader
{
    /// <summary>
    /// Reads a wire contract from the serialized <c>FileDescriptorSet</c> bytes a client or server carries.
    /// </summary>
    /// <param name="descriptorSet">The serialized descriptor set.</param>
    /// <returns>The normalized <see cref="WireContract"/>.</returns>
    public static WireContract Read(ReadOnlyMemory<byte> descriptorSet)
    {
        using var stream = new MemoryStream(descriptorSet.ToArray(), writable: false);
        return Read(ProtoBuf.Serializer.Deserialize<FileDescriptorSet>(stream));
    }

    /// <summary>
    /// Reads a wire contract from a parsed <c>FileDescriptorSet</c>.
    /// </summary>
    /// <param name="descriptorSet">The descriptor set.</param>
    /// <returns>The normalized <see cref="WireContract"/>.</returns>
    public static WireContract Read(FileDescriptorSet descriptorSet)
    {
        var services = new Dictionary<string, WireService>(StringComparer.Ordinal);
        var messages = new Dictionary<string, WireMessage>(StringComparer.Ordinal);
        var enums = new Dictionary<string, WireEnum>(StringComparer.Ordinal);

        foreach (var file in descriptorSet.Files)
        {
            var scope = string.IsNullOrEmpty(file.Package) ? string.Empty : $".{file.Package}";

            foreach (var message in file.MessageTypes)
            {
                AddMessage(message, scope, messages, enums);
            }

            foreach (var @enum in file.EnumTypes)
            {
                AddEnum(@enum, scope, enums);
            }

            foreach (var service in file.Services)
            {
                var fullName = $"{scope}.{service.Name}";
                services[fullName] = new WireService(
                    fullName,
                    service.Methods.ToDictionary(
                        _ => _.Name,
                        _ => new WireMethod(_.Name, _.InputType, _.OutputType, _.ClientStreaming, _.ServerStreaming),
                        StringComparer.Ordinal));
            }
        }

        return new WireContract(services, messages, enums);
    }

    static void AddMessage(
        DescriptorProto message,
        string scope,
        Dictionary<string, WireMessage> messages,
        Dictionary<string, WireEnum> enums)
    {
        var fullName = $"{scope}.{message.Name}";

        messages[fullName] = new WireMessage(
            fullName,
            message.Fields.ToDictionary(_ => _.Number, _ => ReadField(_, message)));

        // Nested types are addressed on the wire through their parent's scope, so they are flattened into the same
        // lookup under their qualified name rather than hung off the parent - a message that moves between nesting
        // levels changes its qualified name, which is exactly the break that should be reported.
        foreach (var nested in message.NestedTypes)
        {
            AddMessage(nested, fullName, messages, enums);
        }

        foreach (var nested in message.EnumTypes)
        {
            AddEnum(nested, fullName, enums);
        }
    }

    static void AddEnum(EnumDescriptorProto @enum, string scope, Dictionary<string, WireEnum> enums)
    {
        var fullName = $"{scope}.{@enum.Name}";

        // Aliased values share a number; the first declared one is the canonical name protobuf uses when it
        // writes the value back out, so that is the one a rename has to be judged against.
        enums[fullName] = new WireEnum(
            fullName,
            @enum.Values.GroupBy(_ => _.Number).ToDictionary(_ => _.Key, _ => _.First().Name));
    }

    static WireField ReadField(FieldDescriptorProto field, DescriptorProto declaringMessage)
    {
        var oneOf = field.ShouldSerializeOneofIndex() && field.OneofIndex >= 0 && field.OneofIndex < declaringMessage.OneofDecls.Count
            ? declaringMessage.OneofDecls[field.OneofIndex].Name
            : null;

        return new WireField(
            field.Number,
            field.Name,
            TypeNameOf(field),
            field.label == FieldDescriptorProto.Label.LabelRepeated ? WireFieldLabel.Repeated : WireFieldLabel.Singular,
            oneOf);
    }

    static string TypeNameOf(FieldDescriptorProto field) =>
        field.type is FieldDescriptorProto.Type.TypeMessage or FieldDescriptorProto.Type.TypeEnum or FieldDescriptorProto.Type.TypeGroup
            ? field.TypeName
            : field.type.ToString()["Type".Length..].ToLowerInvariant();
}
