// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Queries;
using Aksio.Cratis.Timers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for a single instance.
/// </summary>
public class SingleKernelClient : IClient, IDisposable
{
    readonly IHttpClientFactory _clientFactory;
    readonly ITimerFactory _timerFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly SingleKernelOptions _options;
    readonly Uri _clientEndpoint;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IClientLifecycle _clientLifecycle;
    readonly ILogger<SingleKernelClient> _logger;
    readonly MicroserviceId _microserviceId;
    TaskCompletionSource<bool> _connectCompletion;
    ITimer? _timer;

    /// <inheritdoc/>
    public bool IsConnected => _clientLifecycle.IsConnected;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => _clientLifecycle.ConnectionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleKernelClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">An <see cref="IHttpClientFactory"/> to create clients from.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="options">The <see cref="SingleKernelOptions"/> to use for connecting.</param>
    /// <param name="clientEndpoint">The client endpoint.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public SingleKernelClient(
        IHttpClientFactory httpClientFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        SingleKernelOptions options,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<SingleKernelClient> logger)
    {
        _clientFactory = httpClientFactory;
        _timerFactory = timerFactory;
        _executionContextManager = executionContextManager;
        _options = options;
        _clientEndpoint = clientEndpoint;
        _jsonSerializerOptions = jsonSerializerOptions;
        _clientLifecycle = clientLifecycle;
        _logger = logger;
        _microserviceId = _executionContextManager.Current.MicroserviceId;
        _connectCompletion = new TaskCompletionSource<bool>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <inheritdoc/>
    public Task Connect()
    {
        _ = Task.Run(async () =>
        {
            _logger.Connecting(_options.Endpoint.ToString());

            _timer?.Dispose();
            _timer = null;

            var attribute = typeof(SingleKernelClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = attribute?.InformationalVersion ?? "1.0.0";
            var info = new ClientInformation(version, _clientEndpoint.ToString());

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
                await Task.Delay(2000);
            }

            _connectCompletion.SetResult(true);
            await _clientLifecycle.Connected();

            _timer = _timerFactory.Create(_ => Ping().Wait(), 1000, 1000);
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> PerformCommand(string route, object? command = null)
    {
        _logger.PerformingCommand(_options.Endpoint.ToString(), route);
        ThrowIfClientIsDisconnected();
        await _connectCompletion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return await PerformCommandInternal(route, command);
    }

    /// <inheritdoc/>
    public async Task<QueryResult> PerformQuery(string route, IDictionary<string, string>? queryString = null)
    {
        _logger.PerformingQuery(_options.Endpoint.ToString(), route);
        await _connectCompletion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        ThrowIfClientIsDisconnected();

        var client = _clientFactory.CreateClient();
        client.BaseAddress = _options.Endpoint;
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
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(_jsonSerializerOptions);
        LogQueryResult(route, result);

        return result!;
    }

    async Task<CommandResult> PerformCommandInternal(string route, object? command = null)
    {
        var client = _clientFactory.CreateClient();
        client.BaseAddress = _options.Endpoint;
        HttpResponseMessage response;

        if (command is not null)
        {
            response = await client.PostAsJsonAsync(route, command, options: _jsonSerializerOptions);
        }
        else
        {
            response = await client.PostAsync(route, null);
        }
        var result = await response.Content.ReadFromJsonAsync<CommandResult>(_jsonSerializerOptions);
        LogCommandResult(route, result);

        return result!;
    }

    async Task Ping()
    {
        var failed = false;
        try
        {
            var result = await PerformCommandInternal($"/api/clients/{_microserviceId}/ping/{ConnectionId}");
            if (!result.IsSuccess)
            {
                failed = true;
            }
        }
        catch
        {
            failed = true;
        }

        if (failed)
        {
            _logger.KernelDisconnected();
            await _clientLifecycle.Disconnected();

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
                _logger.CommandResultExceptions(route, result.ExceptionMessages);
            }

            if (!result.IsValid)
            {
                foreach (var validationError in result.ValidationErrors)
                {
                    _logger.CommandResultValidationError(route, string.Join(',', validationError.MemberNames), validationError.Message);
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
                _logger.QueryResultExceptions(route, result.ExceptionMessages);
            }

            if (!result.IsValid)
            {
                foreach (var validationError in result.ValidationErrors)
                {
                    _logger.QueryResultValidationError(route, string.Join(',', validationError.MemberNames), validationError.Message);
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
