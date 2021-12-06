// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Events.Projections.Pipelines
{
    public class PipelineJobStatus
    {
        readonly BehaviorSubject<double> _progress = new(0);
        readonly BehaviorSubject<string> _task = new(string.Empty);
        readonly Subject<IProjectionPipelineJobStep> _step = new();

        /// <summary>
        /// Gets the observable for current the step being worked on
        /// </summary>
        public IObservable<IProjectionPipelineJobStep> Step => _step;

        /// <summary>
        /// Gets the observable for the current progress of the current step.
        /// </summary>
        public IObservable<double> Progress => _progress;

        /// <summary>
        /// Gets the observable for the current task being worked on.
        /// </summary>
        public IObservable<string> Task => _task;

        /// <summary>
        /// Report progress for the status.
        /// </summary>
        /// <param name="progress">Current progress to report.</param>
        public void ReportProgress(double progress) => _progress.OnNext(progress);

        /// <summary>
        /// Report task for the status.
        /// </summary>
        /// <param name="task">Current task to report</param>
        public void ReportTask(string task) => _task.OnNext(task);

        /// <summary>
        /// Report step for the status.
        /// </summary>
        /// <param name="step">Current step to report.</param>
        public void ReportStep(IProjectionPipelineJobStep step) => _step.OnNext(step);
    }
}
