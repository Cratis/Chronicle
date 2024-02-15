// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { HTMLAttributes, ReactNode } from 'react';

export interface PageProps extends HTMLAttributes<HTMLDivElement> {
    title: string;
    mainClassName?: string;
    children?: ReactNode;
}

export const Page = ({ title,mainClassName, children, ...rest }: PageProps) => {
    return (
        <div className='px-6 py-4 flex flex-col h-full' {...rest}>
            <h1 className='text-3xl mt-3 mb-4'>{title}</h1>
            <main className={mainClassName ??'flex-1'}>
                {children}
            </main>
        </div>
    );
};
