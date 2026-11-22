// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MdAirlineStops, MdCheckCircle, MdErrorOutline, MdGroupWork, MdInfo } from 'react-icons/md';
import { NamespaceHealth } from './health';
import { Stat, Tile, type ITile } from './Tile';
import { Tone } from './WidgetShell';
import { pollFailure } from './pollFailure';
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
     * Whether the latest health read failed. Retained figures are then stale, not healthy.
     */
    failed?: boolean;

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

const ObserverStats = ({ health, failed }: { health: NamespaceHealth | undefined; failed: boolean }) => {
    if (failed && !health) return null;
    return (
        <>
            <Stat tone={failed ? 'neutral' : 'success'} value={health?.observers.active ?? 0} label={texts.active} />
            <Stat tone='warning' value={health?.observers.replaying ?? 0} label={texts.replaying} />
            <Stat tone='neutral' value={health?.observers.suspended ?? 0} label={texts.suspended} />
            <Stat tone='danger' value={(health?.observers.disconnected ?? 0) + (health?.observers.quarantined ?? 0)} label={texts.disconnectedObservers} />
        </>
    );
};

interface HealthCountTileProps extends Pick<ITile, 'title' | 'icon' | 'caption' | 'link'> {
    count: number | undefined;
    failed: boolean;
    tone: Tone;
    emptyTone?: Tone;
    emptyCaption: string;
}

const countTone = (count: number | undefined, failed: boolean, tone: Tone, emptyTone: Tone): Tone => {
    if (failed || count === undefined) return 'neutral';
    return count > 0 ? tone : emptyTone;
};

const countValue = (count: number | undefined, failed: boolean) => {
    if (count === undefined) return undefined;
    return count > 0 || failed ? count.toLocaleString() : '✓';
};

const HealthCountTile = ({ count, failed, tone, emptyTone = 'success', caption, emptyCaption, link, ...tile }: HealthCountTileProps) => (
    <Tile
        {...tile}
        tone={countTone(count, failed, tone, emptyTone)}
        value={countValue(count, failed)}
        caption={pollFailure(count, failed) ?? (count ? caption : emptyCaption)}
        link={count ? link : undefined} />
);

/**
 * The four metric tiles the CLI workbench shows beside its health rail: observers, failures, recommendations and jobs.
 */
export const HealthTiles = ({ health, failed = false, links }: IHealthTiles) => (
    <>
        <Tile
            title={texts.observers}
            icon={<MdAirlineStops />}
            tone={failed ? 'neutral' : observersTone(health)}
            value={health?.observers.total.toLocaleString()}
            caption={pollFailure(health, failed) ?? texts.observersCaption}
            link={links && { to: links.observers, label: texts.viewObservers }}>
            <ObserverStats health={health} failed={failed} />
        </Tile>

        <HealthCountTile
            title={texts.failures}
            icon={failed || (health?.failedPartitions ?? 0) > 0 ? <MdErrorOutline /> : <MdCheckCircle />}
            count={health?.failedPartitions}
            failed={failed}
            tone='danger'
            caption={texts.failedPartitions}
            emptyCaption={texts.allPartitionsHealthy}
            link={links && { to: links.failedPartitions, label: texts.needsAttention }} />

        <HealthCountTile
            title={texts.recommendations}
            icon={<MdInfo />}
            count={health?.recommendations}
            failed={failed}
            tone='warning'
            caption={texts.pendingRecommendations}
            emptyCaption={texts.noRecommendations}
            link={links && { to: links.recommendations, label: texts.reviewSuggested }} />

        <HealthCountTile
            title={texts.jobs}
            icon={<MdGroupWork />}
            count={health?.runningJobs}
            failed={failed}
            tone='info'
            emptyTone='info'
            caption={texts.jobsRunning}
            emptyCaption={texts.noJobsRunning}
            link={links && { to: links.jobs, label: texts.viewJobs }} />
    </>
);
