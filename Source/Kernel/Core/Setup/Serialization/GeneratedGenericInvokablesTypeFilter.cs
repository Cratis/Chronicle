// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Orleans.Serialization;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Configuration;
using Orleans.Serialization.Invocation;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents an <see cref="ITypeFilter"/> that allows the generic invokable definitions the Orleans code generator registered a serializer for.
/// </summary>
/// <remarks>
/// A grain method with its own type parameters, such as <c language="csharp">IObserver.Subscribe&lt;TObserverSubscriber&gt;</c>, compiles to a generic
/// invokable whose definition the generated manifest registers by its compound alias. Since Orleans 10.4.1 a closed generic type is only
/// allowed when its definition is allowed on its own, and that check looks the definition up by its alias-formatted name, which is not the
/// name the manifest grant was stored under. Without this filter, sending such a request to another silo is refused with
/// "Type ... is not allowed".
/// The filter grants exactly the definitions the manifest already registered a serializer for and decides nothing else, so every
/// generic argument of a closed invokable must still be allowed on its own.
/// </remarks>
/// <param name="options">The <see cref="TypeManifestOptions"/> holding the generated manifest.</param>
public class GeneratedGenericInvokablesTypeFilter(IOptions<TypeManifestOptions> options) : ITypeFilter
{
    readonly Lazy<HashSet<Type>> _definitions = new(() => DefinitionsFrom(options.Value));

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type) =>
        type.IsGenericTypeDefinition && _definitions.Value.Contains(type) ? true : null;

    static HashSet<Type> DefinitionsFrom(TypeManifestOptions options) =>
        options.Serializers
            .SelectMany(serializer => serializer.GetInterfaces())
            .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IFieldCodec<>))
            .Select(@interface => @interface.GenericTypeArguments[0])
            .Where(type => type.IsGenericType && type.ContainsGenericParameters && typeof(IInvokable).IsAssignableFrom(type))
            .Select(type => type.GetGenericTypeDefinition())
            .ToHashSet();
}
