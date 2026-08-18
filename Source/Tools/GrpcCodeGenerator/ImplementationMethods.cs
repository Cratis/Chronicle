// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Writes the members of a generated service implementation.
/// </summary>
public static class ImplementationMethods
{
    const string CommandResultType = "global::Cratis.Chronicle.Contracts.Commands.CommandResult";
    const string QueryResultType = "global::Cratis.Chronicle.Contracts.Queries.QueryResult";
    const string CallContextType = "global::ProtoBuf.Grpc.CallContext";

    /// <summary>
    /// Writes the method that dispatches a gRPC operation to a command.
    /// </summary>
    /// <param name="command">The command to dispatch to.</param>
    /// <param name="context">The generation context.</param>
    /// <returns>The C# source for the method.</returns>
    /// <exception cref="UnsupportedServiceShape">Thrown when the command has no Handle method to dispatch to.</exception>
    public static string ForCommand(CommandDefinition command, ImplementationContext context)
    {
        var handle = command.Handle
            ?? throw new UnsupportedServiceShape(command.Type.FullName ?? command.Name, "it has no Handle method to dispatch to.");

        var hasRequest = command.Parameters.Count > 0;
        var construction = $"new {QualifiedTypeName.For(command.Type)}({string.Join(", ", command.Parameters.Select(RequestArgument))})";
        var invocation = $"command.Handle({string.Join(", ", handle.GetParameters().Select(p => context.Dependencies.NameFor(p.ParameterType)))})";

        var parameters = hasRequest
            ? $"global::{context.ContractsNamespace}.{command.Name}Request request, {CallContextType} callContext = default"
            : $"{CallContextType} callContext = default";

        if (command.ResponseType is not { } responseType)
        {
            var handler = handle.ReturnType == typeof(void)
                ? $"command => {{ {invocation}; return Task.CompletedTask; }}"
                : $"command => {invocation}";

            return Method(
                $"Task<{CommandResultType}>",
                command.Name,
                parameters,
                $"CommandExecutor.Execute(\n            {construction},\n            {handler})");
        }

        var mapping = MappingFor(responseType, command.Name, context);
        var isAsync = handle.ReturnType.IsGenericType && handle.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
        var responseHandler = isAsync
            ? $"async command => {mapping.Apply($"(await {invocation})")}"
            : $"command => Task.FromResult({mapping.Apply(invocation)})";

        return Method(
            $"Task<{CommandResultType}<{mapping.ContractTypeName}>>",
            command.Name,
            parameters,
            $"CommandExecutor.Execute<{QualifiedTypeName.For(command.Type)}, {mapping.ContractTypeName}>(\n            {construction},\n            {responseHandler})");
    }

    /// <summary>
    /// Writes the method that dispatches a gRPC operation to a query.
    /// </summary>
    /// <param name="method">The query method to dispatch to.</param>
    /// <param name="readModelType">The read model the query is declared on.</param>
    /// <param name="serviceName">The name of the service the query belongs to.</param>
    /// <param name="context">The generation context.</param>
    /// <returns>The C# source for the method.</returns>
    public static string ForQuery(QueryMethodDefinition method, Type readModelType, string serviceName, ImplementationContext context)
    {
        var wireParameters = method.Parameters.Where(p => !ParameterClassification.IsDependency(p.ParameterType)).ToList();
        var arguments = method.Parameters.Select(p =>
            ParameterClassification.IsDependency(p.ParameterType)
                ? context.Dependencies.NameFor(p.ParameterType)
                : RequestArgument(p));

        var invocation = $"{QualifiedTypeName.For(readModelType)}.{method.Name}({string.Join(", ", arguments)})";
        var parameters = wireParameters.Count > 0
            ? $"global::{context.ContractsNamespace}.{method.Name}Request request, {CallContextType} callContext = default"
            : $"{CallContextType} callContext = default";

        if (TypeHelper.IsVoidTask(method.ReturnType))
        {
            return Method("Task", method.Name, parameters, invocation);
        }

        var logger = $"exception => logger.QueryFailed(exception, \"{serviceName}\", \"{method.Name}\")";

        if (method.IsObservable)
        {
            var observed = Nullable(ImplementationDataMapping.For(ObservableElement(method), readModelType, context), method);
            var stream = $"{invocation}\n                .CompletedBy(callContext.CancellationToken)";
            if (!observed.IsIdentity)
            {
                stream += $"\n                .Select(_ => ({observed.ContractTypeName}){observed.Apply("_")})";
            }

            return Method(
                $"IObservable<{QueryResultType}<{observed.ContractTypeName}>>",
                method.Name,
                parameters,
                $"QueryExecutor.Execute<{observed.ContractTypeName}>(\n            () => {stream},\n            {logger})");
        }

        var mapping = Nullable(ImplementationDataMapping.For(TypeHelper.GetQueryReturnType(method.ReturnType), readModelType, context), method);

        return Method(
            $"Task<{QueryResultType}<{mapping.ContractTypeName}>>",
            method.Name,
            parameters,
            $"QueryExecutor.Execute<{mapping.ContractTypeName}>(\n            {QueryBody(method, invocation, mapping)},\n            {logger})");
    }

    /// <summary>
    /// Writes the method that copies a domain value onto the contract message that carries it.
    /// </summary>
    /// <param name="mapping">The mapping to write.</param>
    /// <param name="context">The generation context.</param>
    /// <returns>The C# source for the method.</returns>
    public static string ForMapping(ResponseMapping mapping, ImplementationContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"    static {mapping.ContractTypeName} {mapping.MethodName}({QualifiedTypeName.For(mapping.DomainType)} source) =>");
        builder.AppendLine("        new()");
        builder.AppendLine("        {");

        var assignments = mapping.Members.Select(member =>
        {
            var value = ImplementationDataMapping.For(member.Type, null, context);
            var read = $"source.{member.Name}";

            // A member that can be absent is mapped only when it is there; the mappers take a value, not nothing.
            var assigned = value.IsIdentity || !member.IsNullable ? value.Apply(read) : $"{read} is null ? null : {value.Apply(read)}";
            return $"            {member.Name} = {assigned}";
        });

        builder.AppendJoin(",\n", assignments).AppendLine();
        builder.AppendLine("        };");
        return builder.ToString();
    }

    /// <summary>
    /// Writes the lambda that produces a query's data.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="invocation">The expression invoking it.</param>
    /// <param name="mapping">How its data becomes contract data.</param>
    /// <returns>The lambda source.</returns>
    /// <remarks>
    /// The mapped forms bind the result to a local rather than mapping the invocation expression in place. A
    /// null-tolerant mapping reads its value twice, and reading an await twice would run the query twice.
    /// </remarks>
    static string QueryBody(QueryMethodDefinition method, string invocation, ImplementationDataMapping mapping)
    {
        var isSynchronous = QueryNullability.IsSynchronous(method.Method);

        if (mapping.IsIdentity)
        {
            return isSynchronous ? $"() => Task.FromResult<{mapping.ContractTypeName}>({invocation})" : $"() => {invocation}";
        }

        var produce = isSynchronous
            ? $"                var result = {invocation};\n                return Task.FromResult<{mapping.ContractTypeName}>({mapping.Apply("result")});"
            : $"                var result = await {invocation};\n                return {mapping.Apply("result")};";

        return $"{(isSynchronous ? string.Empty : "async ")}() =>\n            {{\n{produce}\n            }}";
    }

    /// <summary>
    /// Widens a mapping to allow the absence the query declares.
    /// </summary>
    /// <param name="mapping">The mapping to widen.</param>
    /// <param name="method">The query method.</param>
    /// <returns>The widened mapping.</returns>
    static ImplementationDataMapping Nullable(ImplementationDataMapping mapping, QueryMethodDefinition method)
    {
        if (mapping.ContractTypeName.EndsWith('?') || !QueryNullability.ResultIsNullable(method.Method))
        {
            return mapping;
        }

        var inner = mapping.Apply;
        return mapping with
        {
            ContractTypeName = $"{mapping.ContractTypeName}?",
            Apply = mapping.IsIdentity ? inner : expression => $"{expression} is null ? null : {inner(expression)}"
        };
    }

    static ImplementationDataMapping MappingFor(Type responseType, string commandName, ImplementationContext context)
    {
        var unwrapped = TypeHelper.UnwrapConceptType(responseType);
        if (unwrapped != responseType || unwrapped.IsPrimitive || unwrapped == typeof(string) || unwrapped == typeof(Guid))
        {
            return ImplementationDataMapping.For(responseType, null, context);
        }

        var mapping = context.MappingForCommandResponse(responseType, commandName);
        return new(mapping.ContractTypeName, expression => $"{mapping.MethodName}({expression})", false, mapping.MethodName);
    }

    static Type ObservableElement(QueryMethodDefinition method)
    {
        var returnType = method.ReturnType;
        if (returnType.IsGenericType)
        {
            return returnType.GetGenericArguments()[0];
        }

        var observable = returnType.GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IObservable`", StringComparison.Ordinal));

        return observable?.GetGenericArguments()[0]
            ?? throw new UnsupportedServiceShape(method.Name, "it is observable but the generator cannot determine what it observes.");
    }

    static string RequestArgument(ParameterInfo parameter) =>
        ImplementationValues.ToDomain($"request.{ImplementationValues.PropertyName(parameter.Name ?? "value")}", parameter.ParameterType);

    static string Method(string returnType, string name, string parameters, string body)
    {
        var builder = new StringBuilder();
        builder.AppendLine("    /// <inheritdoc/>");
        builder.AppendLine($"    public {returnType} {name}({parameters}) =>");
        builder.AppendLine($"        {body};");
        return builder.ToString();
    }
}
