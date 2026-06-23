// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents a discovered handler method on a <see cref="IReadModelReactor"/>.
/// </summary>
/// <param name="ChangeType">The <see cref="ReadModelChangeType"/> the method reacts to.</param>
/// <param name="Method">The <see cref="MethodInfo"/> of the handler method.</param>
/// <param name="ReadModelType">The read model <see cref="Type"/> the method reacts to.</param>
/// <param name="IsCollection">Whether the first parameter is a collection of read models rather than a single instance.</param>
public record ReadModelReactorMethod(ReadModelChangeType ChangeType, MethodInfo Method, Type ReadModelType, bool IsCollection);
