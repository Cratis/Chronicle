// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MdAirlineStops, MdCheckCircle, MdErrorOutline, MdGroupWork, MdInfo } from 'react-icons/md';
import { NamespaceHealth } from './health';
import { Stat, Tile } from './Tile';
import { Tone } from './WidgetShell';
import strings from 'Strings';

/**
 * Where each tile leads to, when there is a single place to act on its figure.
 */
export interface HealthLinks {
    observers: string;
    failedPartitions: string;
    recommendations: string;
    jobs: string;
}

export interface IHealthTiles {

    /**
     * The figures, or undefined while they are loading.
     */
    health: NamespaceHealth | undefined;

    /**
     * Optional links to where each figure can be acted on.
     */
    links?: HealthLinks;
}

const texts = strings.eventStore.dashboard;

const observersTone = (health: NamespaceHealth | undefined): Tone => {
    if (!health) return 'neutral';
    if (health.observers.disconnected > 0 || health.observers.quarantined > 0) return 'danger';
    if (health.observers.replaying > 0 || health.observers.suspended > 0) return 'warning';
    return 'success';
};

/**
 * The four metric tiles the CLI workbench shows beside its health rail: observers, failures, recommendations and jobs.
 */
export const HealthTiles = ({ health, links }: IHealthTiles) => {
    const failed = health?.failedPartitions ?? 0;
    const recommendations = health?.recommendations ?? 0;
    const jobs = health?.runningJobs ?? 0;

    return (
        <>
            <Tile
                title={texts.observers}
                icon={<MdAirlineStops />}
                tone={observersTone(health)}
                value={health?.observers.total.toLocaleString()}
                caption={texts.observersCaption}
                link={links && { to: links.observers, label: texts.viewObservers }}>
                <Stat tone='success' value={health?.observers.active ?? 0} label={texts.active} />
                <Stat tone='warning' value={health?.observers.replaying ?? 0} label={texts.replaying} />
                <Stat tone='neutral' value={health?.observers.suspended ?? 0} label={texts.suspended} />
                <Stat tone='danger' value={(health?.observers.disconnected ?? 0) + (health?.observers.quarantined ?? 0)} label={texts.disconnectedObservers} />
            </Tile>

            <Tile
                title={texts.failures}
                icon={failed > 0 ? <MdErrorOutline /> : <MdCheckCircle />}
                tone={!health ? 'neutral' : failed > 0 ? 'danger' : 'success'}
                value={!health ? undefined : failed > 0 ? failed.toLocaleString() : '✓'}
                caption={failed > 0 ? texts.failedPartitions : texts.allPartitionsHealthy}
                link={links && failed > 0 ? { to: links.failedPartitions, label: texts.needsAttention } : undefined} />

            <Tile
                title={texts.recommendations}
                icon={<MdInfo />}
                tone={!health ? 'neutral' : recommendations > 0 ? 'warning' : 'success'}
                value={!health ? undefined : recommendations > 0 ? recommendations.toLocaleString() : '✓'}
                caption={recommendations > 0 ? texts.pendingRecommendations : texts.noRecommendations}
                link={links && recommendations > 0 ? { to: links.recommendations, label: texts.reviewSuggested } : undefined} />

            <Tile
                title={texts.jobs}
                icon={<MdGroupWork />}
                tone={!health ? 'neutral' : 'info'}
                value={!health ? undefined : jobs > 0 ? jobs.toLocaleString() : '✓'}
                caption={jobs > 0 ? texts.jobsRunning : texts.noJobsRunning}
                link={links && jobs > 0 ? { to: links.jobs, label: texts.viewJobs } : undefined} />
        </>
    );
};
