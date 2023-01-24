// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator.Syntax;

public class PropertyOrParameterSymbol
{
    IParameterSymbol? _parameterSymbol;
    IPropertySymbol? _propertySymbol;

    public PropertyOrParameterSymbol(IParameterSymbol parameterSymbol) => _parameterSymbol = parameterSymbol;
    public PropertyOrParameterSymbol(IPropertySymbol propertySymbol) => _propertySymbol = propertySymbol;
    public string Name => (_parameterSymbol?.Name ?? _propertySymbol?.Name)!;
    public ITypeSymbol Type => (_parameterSymbol?.Type ?? _propertySymbol?.Type)!;
    public NullableAnnotation? NullableAnnotation => _parameterSymbol?.NullableAnnotation ?? _propertySymbol?.NullableAnnotation;
    public IEnumerable<AttributeData> GetAttributes() => _parameterSymbol?.GetAttributes() ?? _parameterSymbol?.GetAttributes() ?? Enumerable.Empty<AttributeData>();
}
