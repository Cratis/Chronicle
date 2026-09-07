// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import { Tag } from '@cratis/components/Display';
import { Card } from 'Components/Card';
import strings from 'Strings';
import { type ObserverRunningStateBreakdown } from '../ObserverRunningStateBreakdown';
import { totalObservers } from '../dashboardAggregations';
import css from './ObserversWidget.module.css';

/**
 * Props for {@link ObserversWidget}.
 */
export interface ObserversWidgetProps {
    /** The observer running-state breakdown to summarize. */
    breakdown: ObserverRunningStateBreakdown;
    /** Called when the user wants to see the full observers page. */
    onViewAll: () => void;
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Summarizes the observers registered on the namespace, broken down by running state.
 */
export const ObserversWidget = ({ breakdown, onViewAll, className }: ObserversWidgetProps) => {
    const strings_ = strings.eventStore.namespaces.dashboard.observers;

    return (
        <Card
            className={className ? `${css.observersWidget} ${className}` : css.observersWidget}
            header={<h3 className={css.title}>{strings_.title}</h3>}>
            <div className={css.total}>
                <span className={css.totalValue}>{totalObservers(breakdown)}</span>
                <span className={css.totalLabel}>{strings_.observers}</span>
            </div>

            <div className={css.breakdown}>
                <Tag value={`${breakdown.active} ${strings.eventStore.namespaces.observers.states.active}`} severity='success' />
                <Tag value={`${breakdown.suspended} ${strings.eventStore.namespaces.observers.states.suspended}`} severity='warn' />
                <Tag value={`${breakdown.replaying} ${strings.eventStore.namespaces.observers.states.replaying}`} severity='info' />
                <Tag value={`${breakdown.disconnected} ${strings.eventStore.namespaces.observers.states.disconnected}`} severity='secondary' />
                <Tag value={`${breakdown.quarantined} ${strings.eventStore.namespaces.observers.states.quarantined}`} severity='danger' />
            </div>

            <Button variant='link' label={strings_.viewAll} onClick={onViewAll} className={css.viewAll} />
        </Card>
    );
};
