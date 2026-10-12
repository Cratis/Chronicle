// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Describes explicit reducer intent independently of handler invocation.
/// </summary>
/// <param name="ReducerType">The observer's CLR type.</param>
/// <param name="ReadModelType">The maintained model.</param>
/// <param name="IsSelfReducing">Whether static model-owned reduction was explicitly selected.</param>
internal sealed record ReducerDescriptor(Type ReducerType, Type ReadModelType, bool IsSelfReducing);
