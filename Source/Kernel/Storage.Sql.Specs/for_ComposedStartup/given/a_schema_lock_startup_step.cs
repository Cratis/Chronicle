// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_ComposedStartup.given;

public class a_schema_lock_startup_step : for_DatabaseSchemaLock.given.a_command_result
{
    protected readonly CancellationTokenSource _cancellation = new();
    protected readonly immediate_time _time = new();
    protected Func<int, int> _resultForAttempt = _ => -1;
    protected int _attempts;
    protected bool _cancelDuringAcquire;
    protected bool _wrapFailure;
    object _startup;
    MethodInfo _runStep;

    void Establish()
    {
        // Exercise the existing private startup step, as the Core startup specs do for Execute.
        // No other startup collaborators are used by this step's retry path.
        var type = typeof(IEventTypes).Assembly.GetType("Orleans.Hosting.ChronicleServerStartupTask", throwOnError: true)!;
        var logger = typeof(NullLogger<>).MakeGenericType(type).GetField("Instance")!.GetValue(null);
        _startup = Activator.CreateInstance(type, [null, null, null, null, null, null, null, null, new SqlStorageErrorClassifier(), null, null, logger, _time, null])!;
        _runStep = type.GetMethod("RunStep", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            _attempts++;
            if (_cancelDuringAcquire)
            {
                await _cancellation.CancelAsync();
                return await Task.FromCanceled<object?>(_cancellation.Token);
            }

            return _resultForAttempt(_attempts);
        });
    }

    protected Task Execute() => (Task)_runStep.Invoke(_startup, ["schema change", (Func<Task>)Acquire, _cancellation.Token])!;

    async Task Acquire()
    {
        try
        {
            await SqlServerDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero, _cancellation.Token);
        }
        catch (DatabaseSchemaLockNotAcquired error) when (_wrapFailure)
        {
            throw new InvalidOperationException("schema setup failed", error);
        }
    }

    void Destroy() => _cancellation.Dispose();
}
