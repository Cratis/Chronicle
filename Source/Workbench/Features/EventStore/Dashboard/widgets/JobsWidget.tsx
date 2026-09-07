// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import { Card } from 'Components/Card';
import strings from 'Strings';
import css from './JobsWidget.module.css';

/**
 * Props for {@link JobsWidget}.
 */
export interface JobsWidgetProps {
    /** The number of jobs currently running. */
    runningCount: number;
    /** The total number of jobs known to the namespace. */
    totalCount: number;
    /** Called when the user wants to see the full jobs page. */
    onViewAll: () => void;
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Shows how many jobs are currently running for the namespace.
 */
export const JobsWidget = ({ runningCount, totalCount, onViewAll, className }: JobsWidgetProps) => {
    const strings_ = strings.eventStore.namespaces.dashboard.jobs;

    return (
        <Card
            className={className ? `${css.jobsWidget} ${className}` : css.jobsWidget}
            header={<h3 className={css.title}>{strings_.title}</h3>}>
            <div className={css.total}>
                <span className={css.totalValue}>{runningCount}</span>
                <span className={css.totalLabel}>{strings_.running}</span>
            </div>
            <span className={css.subtitle}>{`${totalCount} ${strings_.total}`}</span>

            <Button variant='link' label={strings_.viewAll} onClick={onViewAll} className={css.viewAll} />
        </Card>
    );
};
