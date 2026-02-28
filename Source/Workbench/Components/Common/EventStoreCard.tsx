// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'primereact/card';
import { useNavigate } from 'react-router-dom';
import { ImDatabase } from "react-icons/im";

export interface IEventStoreCard {
    logo?: string;
    path?: string;
    title?: string;
    description?: string;
    footer?: React.ReactNode;
}

export function EventStoreCard(props: IEventStoreCard) {
    const { title, path, footer, description } = props;
    const navigate = useNavigate();

    const image = (
        <div className='pl-4' style={{ display: 'flex', alignItems: 'center', height: '100%' }}>
            <ImDatabase size={48} />
        </div>
    );
    const heading = (
        <h1 className='text-2xl cursor-pointer pt-8' onClick={() => navigate(path!)}>
            {title}
        </h1>
    );

    return (
        <Card
            className='m-4 flex border-2 shadow-none w-160 h-50 overflow-hidden text-ellipsis whitespace-nowrap'
            title={heading}
            footer={footer}
            header={image}>
            {description}
        </Card>
    );
}
