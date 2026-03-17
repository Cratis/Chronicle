// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useState, ReactNode, useContext } from 'react';

const isDevelopmentBuild = import.meta.env.CHRONICLE_DEVELOPMENT === 'true';

interface IWorkbenchContext {
    pageTitle: string;
    setPageTitle: (title: string) => void;
    isDevelopment: boolean;
}

const defaultWorkbenchContext: IWorkbenchContext = {
    pageTitle: '',
    setPageTitle: () => null,
    isDevelopment: false,
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
                isDevelopment: isDevelopmentBuild,
            }}>
            {props.children}
        </WorkbenchContext.Provider>
    );
};
