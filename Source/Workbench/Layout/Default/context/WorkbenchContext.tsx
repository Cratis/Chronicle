// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useState, ReactNode, useContext } from 'react';

interface IWorkbenchContext {
    pageTitle: string;
    setPageTitle: (title: string) => void;
}

const defaultWorkbenchContext: IWorkbenchContext = {
    pageTitle: '',
    setPageTitle: () => null,
};

export const WorkbenchContext = createContext<IWorkbenchContext>(defaultWorkbenchContext);

export const useWorkbenchContext = () => useContext(WorkbenchContext);

export const WorkbenchProvider = (props: { children: ReactNode }) => {
    const [pageTitle, setPageTitle] = useState<string>('');

    return (
        <WorkbenchContext.Provider
            value={{
                pageTitle,
                setPageTitle,
            }}>
            {props.children}
        </WorkbenchContext.Provider>
    );
};
