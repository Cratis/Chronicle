// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Tag } from '@cratis/components/Display';
import { Card } from 'Components/Card';
import strings from 'Strings';
import css from './ServerHealthWidget.module.css';

/**
 * Props for {@link ServerHealthWidget}.
 */
export interface ServerHealthWidgetProps {
    /** The event store this dashboard is showing. */
    eventStore: string;
    /** The namespace this dashboard is showing. */
    namespace: string;
    /** Whether the dashboard's queries are currently resolving against the server. */
    isConnected: boolean;
    /** The tail sequence number of the event log, once it has loaded. */
    tailSequenceNumber?: number;
    /** The number of registered event types. */
    eventTypeCount: number;
    /** The number of registered projections. */
    projectionCount: number;
    /** The number of registered read models. */
    readModelCount: number;
    /** The number of webhooks and external services combined. */
    subscriptionCount: number;
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Shows connection status, the current store/namespace context, and catalog counts -
 * the health-and-status overview panel of the event store dashboard.
 */
export const ServerHealthWidget = ({
    eventStore,
    namespace,
    isConnected,
    tailSequenceNumber,
    eventTypeCount,
    projectionCount,
    readModelCount,
    subscriptionCount,
    className
}: ServerHealthWidgetProps) => (
    <Card
        className={className ? `${css.serverHealthWidget} ${className}` : css.serverHealthWidget}
        header={<h3 className={css.title}>{strings.eventStore.namespaces.dashboard.serverHealth.title}</h3>}>
        <Tag
            value={isConnected
                ? strings.eventStore.namespaces.dashboard.serverHealth.connected
                : strings.eventStore.namespaces.dashboard.serverHealth.disconnected}
            severity={isConnected ? 'success' : 'danger'} />

        <div className={css.section}>
            <span className={css.sectionTitle}>{strings.eventStore.namespaces.dashboard.serverHealth.context}</span>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.store}</span>
                <span className={css.value}>{eventStore}</span>
            </div>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.namespace}</span>
                <span className={css.value}>{namespace}</span>
            </div>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.tailSequence}</span>
                <span className={css.value}>{tailSequenceNumber ?? '-'}</span>
            </div>
        </div>

        <div className={css.section}>
            <span className={css.sectionTitle}>{strings.eventStore.namespaces.dashboard.serverHealth.catalog}</span>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.eventTypes}</span>
                <span className={css.value}>{eventTypeCount}</span>
            </div>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.projections}</span>
                <span className={css.value}>{projectionCount}</span>
            </div>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.readModels}</span>
                <span className={css.value}>{readModelCount}</span>
            </div>
            <div className={css.row}>
                <span className={css.label}>{strings.eventStore.namespaces.dashboard.serverHealth.subscriptions}</span>
                <span className={css.value}>{subscriptionCount}</span>
            </div>
        </div>
    </Card>
);
