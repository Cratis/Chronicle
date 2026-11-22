// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Orleans.Metadata;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Observes real Orleans contexts without calling the measured grains and persists lifecycle evidence.</summary>
/// <param name="artifactDirectory">The absolute run artifact directory shared with BenchmarkDotNet.</param>
/// <param name="operation">The measured grain method.</param>
public sealed class ActivationEvidence(string artifactDirectory, string operation) : IConfigureGrainContextProvider, IConfigureGrainContext
{
    readonly ConcurrentDictionary<GrainId, IGrainContext> _contexts = new();
    readonly ConcurrentDictionary<GrainId, int> _counts = new();
    readonly Guid _caseId = Guid.NewGuid();
    GrainId _target;
    GrainActivationMode _mode;
    Guid _previousHost;
    Guid _host;
    ActivationId? _previousActivation;
    ActivationId? _beforeActivation;
    int _beforeCount;

    /// <inheritdoc/>
    public bool TryGetConfigurator(GrainType grainType, GrainProperties properties, out IConfigureGrainContext configurator)
    {
        configurator = this;
        return true;
    }

    /// <inheritdoc/>
    public void Configure(IGrainContext context)
    {
        _contexts[context.GrainId] = context;
        _counts.AddOrUpdate(context.GrainId, 1, (_, count) => count + 1);
    }

    /// <summary>Starts observation for a replacement host, keeping the pre-restart snapshot.</summary>
    public void ResetContexts()
    {
        _contexts.Clear();
        _counts.Clear();
    }

    /// <summary>Snapshots the host/activation before preparation.</summary>
    /// <param name="mode">The requested lifecycle.</param>
    /// <param name="target">The exact measured identity.</param>
    /// <param name="host">The current host generation.</param>
    public void Begin(GrainActivationMode mode, GrainId target, Guid host)
    {
        _mode = mode;
        _target = target;
        _previousHost = host;
        RememberActivation();
    }

    /// <summary>Remembers the successfully primed target before exact deactivation.</summary>
    public void RememberActivation() => _previousActivation = LiveActivation();

    /// <summary>Rejects eager activation before the timed call.</summary>
    /// <param name="host">The prepared host generation.</param>
    public void Capture(Guid host)
    {
        _host = host;
        _beforeActivation = LiveActivation();
        _beforeCount = _counts.GetValueOrDefault(_target);
        var warm = _mode == GrainActivationMode.Warm;
        Require((_beforeActivation is not null) == warm, "Prepared target has the wrong activation state.");
        if (_mode != GrainActivationMode.PostRestart) return;
        Require(_host != _previousHost, "Host was not replaced.");
        Require(_beforeCount == 0, "Replacement host touched the measured target during startup.");
    }

    /// <summary>Checks lifecycle transitions after successful state/durability validation.</summary>
    /// <param name="state">The validated result and seed markers.</param>
    /// <exception cref="InvalidOperationException">The target has no live activation after the operation.</exception>
    public void Verify(object state)
    {
        var activation = LiveActivation() ?? throw new InvalidOperationException("Operation did not leave its target active.");
        var afterCount = _counts.GetValueOrDefault(_target);
        var warm = _mode == GrainActivationMode.Warm;
        var sameActivation = activation == _previousActivation;
        Require(sameActivation == warm, "Operation observed the wrong activation identity.");
        var expectedCount = warm ? _beforeCount : _beforeCount + 1;
        Require(afterCount == expectedCount, "Operation observed the wrong activation count.");
        Write(new
        {
            Kind = "success", CaseId = _caseId, Operation = operation, Mode = _mode.ToString(), Target = _target.ToString(),
            PreviousHost = _previousHost, Host = _host, PreviousActivation = _previousActivation?.ToString(),
            BeforeActivation = _beforeActivation?.ToString(), Activation = activation.ToString(),
            BeforeCount = _beforeCount, AfterCount = afterCount, DurableValidation = "passed", State = state
        });
    }

    /// <summary>Persists failures, including startup and teardown errors, outside timing.</summary>
    /// <param name="phase">The failed lifecycle phase.</param>
    /// <param name="error">The failure.</param>
    public void Failure(string phase, Exception error) => Write(new
    {
        Kind = "failure", CaseId = _caseId, Operation = operation, Mode = _mode.ToString(), Phase = phase,
        Error = error.ToString()
    });

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    ActivationId? LiveActivation() =>
        _contexts.TryGetValue(_target, out var context) && !context.Deactivated.IsCompleted ? context.ActivationId : null;

    void Write(object entry)
    {
        var directory = Path.Combine(artifactDirectory, "lifecycle");
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(entry);
        File.AppendAllText(Path.Combine(directory, $"{operation}-{_mode}-{_caseId:N}.jsonl"), json + Environment.NewLine);
        Console.WriteLine($"ACTIVATION {json}");
    }
}
