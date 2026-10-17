// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Gives a job step under specification the run its background work has in a silo, so that
/// <c>TryInvokeOnActivation</c> admits callbacks, runs them on a stand-in activation scheduler, and refuses them
/// once the run is closed. The run is internal to Cratis.Orleans, so it is reached by reflection.
/// </summary>
public sealed class StepRunForTesting
{
    static readonly Type _runType = typeof(IJobStep).Assembly.GetType("Cratis.Orleans.Jobs.JobStepRun", throwOnError: true)!;
    static readonly PropertyInfo _currentProperty = _runType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)!;
    static readonly MethodInfo _closeAdmission = _runType.GetMethod("CloseAdmission")!;
    static readonly MethodInfo _beginDrain = _runType.GetMethod("BeginDrain")!;
    static readonly object _stopping = Enum.Parse(_runType.Assembly.GetType("Cratis.Orleans.Jobs.JobStepRunPhase", throwOnError: true)!, "Stopping");

    readonly object _run;

    StepRunForTesting(object run, TaskScheduler activation)
    {
        _run = run;
        Activation = activation;
    }

    /// <summary>
    /// Gets the scheduler standing in for the step's activation. Callbacks run on it; nothing else does.
    /// </summary>
    public TaskScheduler Activation { get; }

    /// <summary>
    /// Gets a value indicating whether the calling code is running on the stand-in activation.
    /// </summary>
    public bool IsOnActivation => TaskScheduler.Current == Activation;

    /// <summary>
    /// Starts a run for a job step and makes it the ambient run of the calling execution context.
    /// </summary>
    /// <param name="jobStep">The job step, derived from <c>JobStep&lt;,,&gt;</c>.</param>
    /// <returns>The <see cref="StepRunForTesting"/>.</returns>
    public static StepRunForTesting Begin(object jobStep)
    {
        var activation = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
        var run = Activator.CreateInstance(_runType, 1L, activation)!;
        _runType.GetMethod("Started")!.Invoke(run, null);
        _runType.GetMethod("Running")!.Invoke(run, null);

        var stepType = jobStep.GetType();
        while (!stepType.IsGenericType || stepType.GetGenericTypeDefinition() != typeof(JobStep<,,>))
        {
            stepType = stepType.BaseType!;
        }

        stepType.GetField("_run", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(jobStep, run);
        _currentProperty.SetValue(null, run);
        return new(run, activation);
    }

    /// <summary>
    /// Stops the run as a stop or deactivation does: it cancels the work and stops admitting callbacks.
    /// </summary>
    public void Close()
    {
        _beginDrain.Invoke(_run, [_stopping]);
        _closeAdmission.Invoke(_run, null);
    }
}
