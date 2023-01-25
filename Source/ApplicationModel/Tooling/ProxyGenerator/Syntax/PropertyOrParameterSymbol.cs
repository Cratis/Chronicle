// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator.Syntax;

/// <summary>
/// Represents either a <see cref="IParameterSymbol"/> or <see cref="IPropertySymbol"/>.
/// </summary>
public class PropertyOrParameterSymbol
{
    readonly IParameterSymbol? _parameterSymbol;
    readonly IPropertySymbol? _propertySymbol;

    /// <summary>
    /// Gets the name of the symbol.
    /// </summary>
    public string Name => (_parameterSymbol?.Name ?? _propertySymbol?.Name)!;

    /// <summary>
    /// Get the type of the symbol.
    /// </summary>
    public ITypeSymbol Type => (_parameterSymbol?.Type ?? _propertySymbol?.Type)!;

    /// <summary>
    /// Gets the nullable annotation setting.
    /// </summary>
    public NullableAnnotation? NullableAnnotation => _parameterSymbol?.NullableAnnotation ?? _propertySymbol?.NullableAnnotation;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyOrParameterSymbol"/> class.
    /// </summary>
    /// <param name="parameterSymbol"><see cref="IParameterSymbol"/> it represents.</param>
    public PropertyOrParameterSymbol(IParameterSymbol parameterSymbol) => _parameterSymbol = parameterSymbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyOrParameterSymbol"/> class.
    /// </summary>
    /// <param name="propertySymbol"><see cref="IPropertySymbol"/> it represents.</param>
    public PropertyOrParameterSymbol(IPropertySymbol propertySymbol) => _propertySymbol = propertySymbol;

    /// <summary>
    /// Get all the attributes, if any for the symbol.
    /// </summary>
    /// <returns>Collection of attributes.</returns>
    public IEnumerable<AttributeData> GetAttributes() => _parameterSymbol?.GetAttributes() ?? _parameterSymbol?.GetAttributes() ?? Enumerable.Empty<AttributeData>();
}
