// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import { Tag } from '@cratis/components/Display';
import { Card } from 'Components/Card';
import strings from 'Strings';
import css from './FailuresWidget.module.css';

/**
 * Props for {@link FailuresWidget}.
 */
export interface FailuresWidgetProps {
    /** The number of currently failed partitions. */
    failedPartitionCount: number;
    /** Called when the user wants to see the full failed partitions page. */
    onViewAll: () => void;
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Shows whether the namespace has any failed partitions - an event store is considered
 * healthy when this count is zero.
 */
export const FailuresWidget = ({ failedPartitionCount, onViewAll, className }: FailuresWidgetProps) => {
    const strings_ = strings.eventStore.namespaces.dashboard.failures;
    const isHealthy = failedPartitionCount === 0;

    return (
        <Card
            className={className ? `${css.failuresWidget} ${className}` : css.failuresWidget}
            header={<h3 className={css.title}>{strings_.title}</h3>}>
            <Tag
                value={isHealthy ? strings_.healthy : `${failedPartitionCount} ${strings_.unhealthy}`}
                severity={isHealthy ? 'success' : 'danger'} />

            {!isHealthy && <Button variant='link' label={strings_.viewAll} onClick={onViewAll} className={css.viewAll} />}
        </Card>
    );
};
