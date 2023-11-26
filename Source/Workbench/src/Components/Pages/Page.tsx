// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode } from 'react';

export interface PageProps {
    title: string;
    children: ReactNode;
}

export const Page = (props: PageProps) => {
    return (
        <div className='p-4'>
            <h1 className='text-3xl m-3'>{props.title}</h1>
            {props.children}
        </div>
    )
}
