// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import { Tag } from '@cratis/components/Display';
import { Card } from 'Components/Card';
import strings from 'Strings';
import css from './RecommendationsWidget.module.css';

/**
 * Props for {@link RecommendationsWidget}.
 */
export interface RecommendationsWidgetProps {
    /** The number of currently pending recommendations. */
    pendingCount: number;
    /** Called when the user wants to see the full recommendations page. */
    onViewAll: () => void;
    /** Applied to the widget's root card. */
    className?: string;
}

/**
 * Shows the number of pending recommendations for the namespace.
 */
export const RecommendationsWidget = ({ pendingCount, onViewAll, className }: RecommendationsWidgetProps) => {
    const strings_ = strings.eventStore.namespaces.dashboard.recommendations;
    const hasPending = pendingCount > 0;

    return (
        <Card
            className={className ? `${css.recommendationsWidget} ${className}` : css.recommendationsWidget}
            header={<h3 className={css.title}>{strings_.title}</h3>}>
            <div className={css.total}>
                <span className={css.totalValue}>{pendingCount}</span>
                <span className={css.totalLabel}>{strings_.pending}</span>
            </div>

            {hasPending && <Tag value={strings_.reviewSuggested} severity='warn' />}

            <Button variant='link' label={strings_.viewAll} onClick={onViewAll} className={css.viewAll} />
        </Card>
    );
};
