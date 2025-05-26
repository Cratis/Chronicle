// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Applications.Commands;
using Cratis.Applications.Queries;
using Cratis.Json;
namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Extension methods for <see cref="HttpClient"/>.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Executes a command.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <param name="command">The command.</param>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <returns>The command result.</returns>
    public static Task<CommandResult<object>?> ExecuteCommand<TCommand>(this HttpClient client, string requestUri, TCommand command)
        => ExecuteCommand<TCommand, object>(client, requestUri, command);

    /// <summary>
    /// Executes a command.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TCommandResult">The type of the command result.</typeparam>
    /// <param name="client">The http client.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <param name="command">The command.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<CommandResult<TCommandResult>?> ExecuteCommand<TCommand, TCommandResult>(this HttpClient client, string requestUri, TCommand command)
    {
        var response = await PostCommand(client, requestUri, command);
        CommandResult<TCommandResult>? commandResult = null;
        try
        {
            commandResult = await response.Content.ReadFromJsonAsync<CommandResult<TCommandResult>>(options: Globals.JsonSerializerOptions);
        }
        catch (Exception)
        {
        }
        return commandResult;
    }

    /// <summary>
    /// Posts a command request.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="client">The http client.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <param name="command">The command.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<HttpResponseMessage> PostCommand<TCommand>(this HttpClient client, string requestUri, TCommand command)
    {
        using var request = JsonContent.Create(command, options: Globals.JsonSerializerOptions);
        return await client.PostAsync(requestUri, request);
    }

    /// <summary>
    /// Executes a query.
    /// </summary>
    /// <param name="client">The http client.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static Task<QueryResult<object>?> ExecuteQuery(this HttpClient client, string requestUri)
        => ExecuteQuery<object>(client, requestUri);

    /// <summary>
    /// Executes a query.
    /// </summary>
    /// <typeparam name="TQueryResult">The type of the query result.</typeparam>
    /// <param name="client">The http client.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<QueryResult<TQueryResult>?> ExecuteQuery<TQueryResult>(this HttpClient client, string requestUri)
    {
        var response = await DoQuery(client, requestUri);
        QueryResult<TQueryResult>? queryResult = null;
        try
        {
            queryResult = await response.Content.ReadFromJsonAsync<QueryResult<TQueryResult>>(options: Globals.JsonSerializerOptions);
        }
        catch (Exception)
        {
        }
        return queryResult;
    }


    /// <summary>
    /// Performs the query get request.
    /// </summary>
    /// <param name="client">The http client.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<HttpResponseMessage> DoQuery(this HttpClient client, string requestUri) =>
        await client.GetAsync(requestUri);

    /// <summary>
    /// Observers a query.
    /// </summary>
    /// <param name="client">The http client.</param>
    /// <param name="requestUri">The request uri string.</param>
    /// <typeparam name="T">The result.</typeparam>
    /// <returns>The <see cref="ISubject{T}"/>.</returns>
    public static ISubject<T> Observe<T>(this HttpClient client, string requestUri)
    {
        var subject = new Subject<T>();
        Task.Run(() => ExecuteObserveRequest(client, subject, requestUri));
        return subject;
    }

    static async Task ExecuteObserveRequest<T>(this HttpClient httpClient, Subject<T> subject, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                continue;
            }

            var results = JsonSerializer.Deserialize<T>(line, Globals.JsonSerializerOptions);
            if (results is not null)
            {
                subject.OnNext(results);
            }
        }

        subject.OnCompleted();
    }
}
