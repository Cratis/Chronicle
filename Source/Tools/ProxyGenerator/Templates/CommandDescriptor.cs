// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes a command for templating purposes.
/// </summary>
/// <param name="Type">The controller type that owns the command.</param>
/// <param name="Method">The method that represents the command.</param>
/// <param name="Route">API route for the command.</param>
/// <param name="Name">Name of the command.</param>
/// <param name="Properties">Properties on the command.</param>
/// <param name="Imports">Additional import statements.</param>
/// <param name="Arguments">Arguments for the request - typically in the route or query string.</param>
/// <param name="HasResponse">Whether or not there is a response from the command.</param>
/// <param name="ResponseType">The details about the response type.</param>
/// <param name="TypesInvolved">Collection of types involved in the command.</param>
public record CommandDescriptor(
    Type Type,
    MethodInfo Method,
    string Route,
    string Name,
    IEnumerable<PropertyDescriptor> Properties,
    IEnumerable<ImportStatement> Imports,
    IEnumerable<RequestArgumentDescriptor> Arguments,
    bool HasResponse,
    ModelDescriptor ResponseType,
    IEnumerable<Type> TypesInvolved) : IDescriptor;
