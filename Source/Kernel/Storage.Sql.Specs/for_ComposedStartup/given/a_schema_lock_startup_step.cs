// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_ComposedStartup.given;

public class a_schema_lock_startup_step : for_DatabaseSchemaLock.given.a_command_result
{
    protected readonly CancellationTokenSource _cancellation = new();
    protected readonly immediate_time _time = new();
    protected Func<int, int> _resultForAttempt = _ => -1;
    protected int _attempts;
    protected bool _cancelDuringAcquire;
    protected bool _wrapFailure;
    Func<string, Func<Task>, CancellationToken, int, bool, Task> _runStep;

    void Establish()
    {
        // Exercise the existing private startup step, as the Core startup specs do for Execute.
        // No other startup collaborators are used by this step's retry path. The task is constructed
        // with named arguments so a change to its constructor breaks this spec at compile time.
        var busySilos = Substitute.For<IBusySilos>();
        busySilos.FindBusySilo(Arg.Any<Exception>()).Returns((BusySilo?)null);
        var startup = new ChronicleServerStartupTask(
            storage: null!,
            eventTypes: null!,
            reactors: null!,
            patternCapture: null!,
            projectionsServiceClient: null!,
            kernelProjections: null!,
            grainFactory: null!,
            authenticationService: null!,
            storageErrorClassifier: new SqlStorageErrorClassifier(),
            patchRunner: null!,
            clusterReleases: null!,
            kernelProjectionRetries: null!,
            busySilos: busySilos,
            options: Options.Create(new Configuration.ChronicleOptions()),
            logger: NullLogger<ChronicleServerStartupTask>.Instance,
            timeProvider: _time);

        // Bound as a typed delegate, so a changed RunStep signature fails here with a clear message
        // rather than as a parameter-count mismatch inside the specification.
        var runStep = typeof(ChronicleServerStartupTask).GetMethod("RunStep", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ChronicleServerStartupTask), "RunStep");
        _runStep = runStep.CreateDelegate<Func<string, Func<Task>, CancellationToken, int, bool, Task>>(startup);
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

    protected Task Execute() => _runStep("schema change", Acquire, _cancellation.Token, 1, true);

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
