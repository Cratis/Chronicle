// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Queries;
using Aksio.Cratis.Tasks;
using Aksio.Cratis.Timers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents a base implementation of <see cref="IClient"/> for REST based clients.
/// </summary>
public abstract class RestKernelClient : IClient, IDisposable
{
    readonly ITaskFactory _taskFactory;
    readonly ITimerFactory _timerFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly Uri _clientEndpoint;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IClientLifecycle _clientLifecycle;
    readonly ILogger<RestKernelClient> _logger;
    readonly MicroserviceId _microserviceId;
    TaskCompletionSource<bool> _connectCompletion;
    ITimer? _timer;

    /// <inheritdoc/>
    public bool IsConnected => _clientLifecycle.IsConnected;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => _clientLifecycle.ConnectionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestKernelClient"/> class.
    /// </summary>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clientEndpoint">The client endpoint.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    protected RestKernelClient(
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<RestKernelClient> logger)
    {
        _taskFactory = taskFactory;
        _timerFactory = timerFactory;
        _executionContextManager = executionContextManager;
        _clientEndpoint = clientEndpoint;
        _jsonSerializerOptions = jsonSerializerOptions;
        _clientLifecycle = clientLifecycle;
        _logger = logger;
        _microserviceId = ExecutionContextManager.GlobalMicroserviceId;
        _connectCompletion = new TaskCompletionSource<bool>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <inheritdoc/>
    public virtual Task Connect()
    {
        _ = _taskFactory.Run(async () =>
        {
            _logger.Connecting();

            _timer?.Dispose();
            _timer = null;

            var attribute = typeof(SingleKernelClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = attribute?.InformationalVersion ?? "1.0.0";
            var info = new ClientInformation(version, _clientEndpoint.ToString(), Debugger.IsAttached);

            for (; ; )
            {
                try
                {
                    var result = await PerformCommandInternal($"/api/clients/{_microserviceId}/connect/{ConnectionId}", info);
                    if (result.IsSuccess)
                    {
                        break;
                    }
                }
                catch
                {
                }
                _logger.KernelUnavailable();
                await OnKernelUnavailable();
                await _taskFactory.Delay(2000);
            }

            _connectCompletion.SetResult(true);
            await _clientLifecycle.Connected();
            _logger.KernelConnected();

            if (!Debugger.IsAttached)
            {
                _timer ??= _timerFactory.Create(_ => Ping().Wait(), 1000, 1000);
            }
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Disconnect()
    {
        _timer?.Dispose();
        _timer = null;

        await PerformCommandInternal($"/api/clients/{_microserviceId}/disconnect/{ConnectionId}");
    }

    /// <inheritdoc/>
    public async Task<CommandResult> PerformCommand(string route, object? command = null, object? metadata = default)
    {
        metadata ??= new object();
        var metadataDictionary = (metadata.AsExpandoObject() as IDictionary<string, object>)!;

        using var scope = _logger.BeginScope(metadataDictionary);
        _logger.PerformingCommand(route);
        ThrowIfClientIsDisconnected();
        await _connectCompletion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return await PerformCommandInternal(route, command);
    }

    /// <inheritdoc/>
    public async Task<TypedQueryResult<TResult>> PerformQuery<TResult>(string route, IDictionary<string, string>? queryString = null, object? metadata = default)
    {
        metadata ??= new object();
        var metadataDictionary = (metadata.AsExpandoObject() as IDictionary<string, object>)!;

        using var scope = _logger.BeginScope(metadataDictionary);

        _logger.PerformingQuery(route);
        await _connectCompletion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        ThrowIfClientIsDisconnected();

        var client = CreateReadHttpClient();
        HttpResponseMessage response;

        if (queryString is not null)
        {
            var uri = QueryHelpers.AddQueryString(route, queryString!);
            response = await client.GetAsync(uri);
        }
        else
        {
            response = await client.GetAsync(route);
        }
        var result = (await response.Content.ReadFromJsonAsync<QueryResult>(_jsonSerializerOptions))!;
        LogQueryResult(route, result);
        var element = (JsonElement)result.Data;
        var deserializedData = element.Deserialize<TResult>(_jsonSerializerOptions)!;

        return new TypedQueryResult<TResult>
        {
            IsAuthorized = result.IsAuthorized,
            ValidationResults = result.ValidationResults,
            ExceptionMessages = result.ExceptionMessages,
            ExceptionStackTrace = result.ExceptionStackTrace,
            Data = deserializedData
        };
    }

    /// <summary>
    /// Create a <see cref="HttpClient"/>.
    /// </summary>
    /// <returns><see cref="HttpClient"/> ready to be used.</returns>
    protected abstract HttpClient CreateHttpClient();

    /// <summary>
    /// Gets called if the client is disconnected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected virtual Task OnDisconnected() => Task.CompletedTask;

    /// <summary>
    /// Gets called if the kernel is unavailable.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected virtual Task OnKernelUnavailable() => Task.CompletedTask;

    async Task<CommandResult> PerformCommandInternal(string route, object? command = null, bool logResult = true)
    {
        using var client = CreateHttpClient();
        HttpResponseMessage response;

        if (command is not null)
        {
            response = await client.PostAsJsonAsync(route, command, options: _jsonSerializerOptions);
        }
        else
        {
            response = await client.PostAsync(route, null);
        }
        var resultAsString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CommandResult>(resultAsString, _jsonSerializerOptions);

        if (logResult) LogCommandResult(route, result);

        return result!;
    }

    HttpClient CreateReadHttpClient()
    {
        var client = CreateHttpClient();
        if (_executionContextManager.IsInContext)
        {
            client.DefaultRequestHeaders.Add(ExecutionContextAppBuilderExtensions.TenantIdHeader, _executionContextManager.Current.TenantId.ToString());
        }
        return client;
    }

    async Task Ping()
    {
        bool failed;
        try
        {
            var result = await PerformCommandInternal($"/api/clients/{_microserviceId}/ping/{ConnectionId}", logResult: false);
            failed = !result.IsSuccess;
        }
        catch
        {
            failed = true;
        }

        if (failed)
        {
            _logger.KernelDisconnected();
            await _clientLifecycle.Disconnected();
            await OnDisconnected();

            _connectCompletion.TrySetCanceled();
            _connectCompletion = new();
            await Connect();
        }
    }

    void LogCommandResult(string route, CommandResult? result)
    {
        _logger.CommandResult(route, result?.IsSuccess ?? false);
        if (result?.IsSuccess == false)
        {
            if (result.HasExceptions)
            {
                _logger.CommandResultExceptions(route, result.ExceptionMessages, result.ExceptionStackTrace);
            }

            if (!result.IsValid)
            {
                foreach (var validationResult in result.ValidationResults)
                {
                    _logger.CommandResultValidationResult(route, string.Join(',', validationResult.Members), validationResult.Message);
                }
            }
        }
    }

    void LogQueryResult(string route, QueryResult? result)
    {
        _logger.QueryResult(route, result?.IsSuccess ?? false);
        if (result?.IsSuccess == false)
        {
            if (result.HasExceptions)
            {
                _logger.QueryResultExceptions(route, result.ExceptionMessages, result.ExceptionStackTrace);
            }

            if (!result.IsValid)
            {
                foreach (var validationResult in result.ValidationResults)
                {
                    _logger.QueryResultValidationResult(route, string.Join(',', validationResult.Members), validationResult.Message);
                }
            }
        }
    }

    void ThrowIfClientIsDisconnected()
    {
        if (!IsConnected)
        {
            throw new DisconnectedClient();
        }
    }
}
