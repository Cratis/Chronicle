// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { HTMLAttributes, ReactNode } from 'react';

import { tw } from 'typewind';

export interface PageProps extends HTMLAttributes<HTMLDivElement> {
    title: string;
    mainClassName?: string;
    children?: ReactNode;
}

export const Page = ({ title,mainClassName, children, ...rest }: PageProps) => {
    return (
        <div className={tw.px_6.py_4.flex.flex_col.h_full} {...rest}>
            <h1 className={tw.text_3xl.mt_3.mb_4}>{title}</h1>
            <main className={mainClassName ??'flex-1'}>
                {children}
            </main>
        </div>
    );
};
