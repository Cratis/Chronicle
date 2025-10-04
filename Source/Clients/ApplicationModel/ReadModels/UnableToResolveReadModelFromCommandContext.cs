// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.ReadModels;

/// <summary>
/// Exception that gets thrown when not being able to resolve a read model from command context.
/// </summary>
/// <param name="readModelType">Type of read model that could not be resolved.</param>
public class UnableToResolveReadModelFromCommandContext(Type readModelType)
    : Exception($"Unable to resolve read model of type '{readModelType.FullName}' from command context. Make sure the command has a property that is assignable to EventSourceId or marked with [Key] attribute")
{
    /// <summary>
    /// Gets the read model type that could not be resolved.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;
}
