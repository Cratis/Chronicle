// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { HTMLAttributes, ReactNode, useEffect } from 'react';
import { useWorkbenchContext } from '../../Layout/Default/context/WorkbenchContext';

export interface PageProps extends HTMLAttributes<HTMLDivElement> {
    title: string;
    children?: ReactNode;
}

export const Page = ({ title, children, ...rest }: PageProps) => {
    const { setPageTitle } = useWorkbenchContext();

    useEffect(() => {
        setPageTitle(title);
    }, [title, setPageTitle]);

    return (
        <div className='px-6 py-4 flex flex-col h-full' {...rest}>
            <main className={`panel overflow-hidden h-full flex flex-col flex-1`}>
                {children}
            </main>
        </div>
    );
};
