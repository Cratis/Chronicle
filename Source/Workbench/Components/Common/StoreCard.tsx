// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Image } from 'primereact/image';
import { Card } from 'primereact/card';
import { useNavigate } from 'react-router-dom';

import { tw } from 'typewind';

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
        <div className={tw.w_24.h_24}>
            <Image alt='Card' src={logo} />
        </div>
    );
    const heading = (
        <h1 className={tw.text_2xl.cursor_pointer} onClick={() => navigate(path!)}>
            {' '}
            {title}
        </h1>
    );

    return (
        <div className={tw.m_4}>
            <Card
                className={tw.flex.p_2.border_2.shadow_none}
                title={heading}
                footer={footer}
                header={image}>
                {description}
            </Card>
        </div>
    );
}
