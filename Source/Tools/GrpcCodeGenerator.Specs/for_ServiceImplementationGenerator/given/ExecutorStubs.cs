// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

/// <summary>
/// The surface a generated implementation calls into, as source the specs compile alongside it.
/// </summary>
/// <remarks>
/// The real executors are internal to the kernel, and pulling the kernel in here to reach them would make the
/// generator's specs depend on the thing the generator generates for. What these specs are actually checking is
/// that the two generators agree - that the class the implementation generator writes satisfies the interface
/// the interface generator writes, signature for signature. That the implementation also compiles against the
/// real executors is checked on every kernel build, which is where it belongs.
/// </remarks>
public static class ExecutorStubs
{
    /// <summary>
    /// Gets the source declaring the executors and log messages a generated implementation refers to.
    /// </summary>
    public const string Source = """
        namespace Cratis.Chronicle.Services;

        using Cratis.Chronicle.Contracts.Commands;
        using Cratis.Chronicle.Contracts.Queries;
        using Microsoft.Extensions.Logging;

        internal static class CommandExecutor
        {
            internal static Task<CommandResult> Execute<TCommand>(TCommand command, Func<TCommand, Task> handle)
                where TCommand : notnull => throw new NotSupportedException();

            internal static Task<CommandResult<TResponse>> Execute<TCommand, TResponse>(TCommand command, Func<TCommand, Task<TResponse>> handle)
                where TCommand : notnull => throw new NotSupportedException();
        }

        internal static class QueryExecutor
        {
            internal static IObservable<QueryResult<TData>> Execute<TData>(Func<IObservable<TData>> query, Action<Exception>? onError = null) =>
                throw new NotSupportedException();

            internal static Task<QueryResult<TData>> Execute<TData>(Func<Task<TData>> query, Action<Exception>? onError = null) =>
                throw new NotSupportedException();
        }

        internal static class ServiceLogMessages
        {
            internal static void QueryFailed(this ILogger logger, Exception exception, string service, string query)
            {
            }
        }
        """;
}
