// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grpc;

/// <summary>
/// Attribute used on a grain interface to declare the type its grain key is constructed from.
/// </summary>
/// <typeparam name="TKey">The type whose constructor parameters, in order, make up the grain key.</typeparam>
/// <remarks>
/// Pairs with <see cref="QueryAttribute"/> on individual grain methods: the generator reads <typeparamref name="TKey"/>'s
/// constructor parameters to build the request fields a keyed query needs to resolve the grain, and combines them with
/// <c>KeyHelper.Combine</c> - the same construction the key type's own <c>ToString()</c> is expected to use - rather
/// than assuming the key type's string conversion happens to agree.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class KeyedByAttribute<TKey> : Attribute
    where TKey : notnull;
