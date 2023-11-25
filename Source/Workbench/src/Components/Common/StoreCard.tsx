// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Image } from 'primereact/image';
import { Card } from 'primereact/card';
import { useNavigate } from 'react-router-dom';

export interface IStoreCard {
    logo?: string;
    path?: string;
    title?: string;
    description?: string;
    footer?: React.ReactNode;
}

export function StoreCard(props: IStoreCard) {
    const { logo, title, path, footer, description } = props;
    const navigate = useNavigate();

    const image = (
        <div className='w-24 h-24 '>
            <Image alt='Card' src={logo} />
        </div>
    );
    const heading = (
        <h1 className='text-2xl cursor-pointer' onClick={() => navigate(path!)}>
            {' '}
            {title}
        </h1>
    );

    return (
        <div className='m-4'>
            <Card
                className='flex p-2 border-2 shadow-none'
                title={heading}
                footer={footer}
                header={image}
            >
                {description}
            </Card>
        </div>
    );
}
