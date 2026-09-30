// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link } from 'react-router-dom';
import { MdGroupWork } from 'react-icons/md';
import { JobSummary } from 'Features/Jobs';
import { isJobRunning } from './health';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IJobsWidget {

    /**
     * The jobs of the namespace, or undefined while loading.
     */
    jobs: JobSummary[] | undefined;

    /**
     * The path the namespace pages live under.
     */
    namespacePath: string;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

/**
 * How many jobs the widget lists.
 */
const shown = 6;

/**
 * The jobs still running on a namespace, with how far along each is.
 */
export const JobsWidget = ({ jobs, namespacePath, className }: IJobsWidget) => {
    const running = (jobs ?? []).filter(isJobRunning);

    return (
        <WidgetShell
            title={texts.runningJobs}
            icon={<MdGroupWork />}
            tone='info'
            subtitle={jobs === undefined ? texts.loading : texts.runningJobsSummary.replace('{count}', running.length.toLocaleString())}
            className={className}>
            {jobs !== undefined && running.length === 0 && <span className='dashboard-widget__empty'>{texts.noJobsRunning}</span>}
            {running.length > 0 && (
                <div className='dashboard-list'>
                    {running.slice(0, shown).map(job => {
                        const total = Number(job.progress?.totalSteps ?? 0);
                        const done = Number(job.progress?.successfulSteps ?? 0) + Number(job.progress?.failedSteps ?? 0);
                        const percent = total > 0 ? Math.round((done / total) * 100) : 0;
                        return (
                            <Link key={job.id.toString()} to={`${namespacePath}/jobs`} className='dashboard-list__item dashboard-list__item--info'>
                                <span className='dashboard-list__text'>
                                    <span className='dashboard-list__title' title={job.type}>{job.type}</span>
                                    <span className='dashboard-list__detail' title={job.details}>{job.details}</span>
                                    <div className='dashboard-meter'>
                                        <div className='dashboard-meter__fill' style={{ width: `${percent}%` }} />
                                    </div>
                                </span>
                                <span className='dashboard-list__aside'>{total > 0 ? `${done}/${total}` : '–'}</span>
                            </Link>
                        );
                    })}
                </div>
            )}
        </WidgetShell>
    );
};
