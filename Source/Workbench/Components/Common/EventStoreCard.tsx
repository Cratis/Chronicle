// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'Components/Card';
import { Link } from 'react-router-dom';
import { ImDatabase } from 'react-icons/im';
import './EventStoreCard.css';

export interface IEventStoreCard {
    logo?: string;
    path?: string;
    title?: string;
    description?: string;
    footer?: React.ReactNode;
}

export function EventStoreCard(props: IEventStoreCard) {
    const { title, path, footer, description } = props;

    return (
        <Card className='workbench-event-store-card m-4 border-2 shadow-none w-160 h-50' footer={footer}>
            <Link to={path!} className='workbench-event-store-card__link'>
                <ImDatabase size={48} aria-hidden='true' />
                <span className='workbench-event-store-card__title'>{title}</span>
            </Link>
            {description}
        </Card>
    );
}
