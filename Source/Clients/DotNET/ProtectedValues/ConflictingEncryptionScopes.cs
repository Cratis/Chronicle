// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The exception that is thrown when a property and its type specify different encryption scopes.
/// </summary>
/// <param name="property">The name of the property carrying conflicting scopes.</param>
/// <param name="typeScope">The encryption scope declared on the property's type.</param>
/// <param name="propertyScope">The encryption scope declared on the property or its constructor parameter.</param>
public sealed class ConflictingEncryptionScopes(string property, EncryptionScope typeScope, EncryptionScope propertyScope)
    : Exception($"'{property}' specifies conflicting [Encrypted] scopes: its type uses '{typeScope}' and its property or constructor parameter uses '{propertyScope}'. A value needs exactly one encryption scope; use the same scope on both declarations.");
