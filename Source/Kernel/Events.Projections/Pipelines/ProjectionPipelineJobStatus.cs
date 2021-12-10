// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents the status of a <see cref="IProjectionPipelineJob"/>.
    /// </summary>
    public class ProjectionPipelineJobStatus
    {
        readonly Subject<IProjectionPipelineJobStep> _step = new();

        /// <summary>
        /// Gets the observable for current the step being worked on
        /// </summary>
        public IObservable<IProjectionPipelineJobStep> Step => _step;

        /// <summary>
        /// Gets the observable for the current progress of the current step.
        /// </summary>
        public BehaviorSubject<float> Progress { get; } = new(0);

        /// <summary>
        /// Gets the observable for the current task being worked on.
        /// </summary>
        public BehaviorSubject<string> Task { get; } = new(string.Empty);

        /// <summary>
        /// Gets the observable for the current state of the job (active or not).
        /// </summary>
        public BehaviorSubject<bool> Active { get; } = new(false);

        /// <summary>
        /// Report progress for the status.
        /// </summary>
        /// <param name="progress">Current progress to report.</param>
        public void ReportProgress(float progress) => Progress.OnNext(progress);

        /// <summary>
        /// Report task for the status.
        /// </summary>
        /// <param name="task">Current task to report</param>
        public void ReportTask(string task) => Task.OnNext(task);

        /// <summary>
        /// Report step for the status.
        /// </summary>
        /// <param name="step">Current step to report.</param>
        public void ReportStep(IProjectionPipelineJobStep step) => _step.OnNext(step);

        /// <summary>
        /// Report that the job has started.
        /// </summary>
        public void ReportStarted()
        {
            Active.OnNext(true);
        }

        /// <summary>
        /// Report that the job is stopped.
        /// </summary>
        public void ReportStopped()
        {
            Task.OnNext("Stopped");
            Active.OnNext(false);
        }
    }
}
