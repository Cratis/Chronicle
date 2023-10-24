import { createContext, useState, ReactNode, useContext } from 'react';

interface ILayoutConfig {
    leftSidebarOpen: boolean;
}

interface ILayoutContext {
    layoutConfig: ILayoutConfig;
    setLayoutConfig: (config: ILayoutConfig) => void;
    toggleLeftSidebar: () => void;
}

const defaultLayoutContext: ILayoutContext = {
    layoutConfig: {
        leftSidebarOpen: true
    },
    setLayoutConfig: () => {
    },
    toggleLeftSidebar: () => {
    }
};

export const LayoutContext = createContext<ILayoutContext>(defaultLayoutContext);
export const useLayoutContext = () => useContext(LayoutContext);
export const LayoutProvider = (props: { children: ReactNode }) => {

    const [layoutConfig, setLayoutConfig] = useState<ILayoutConfig>(defaultLayoutContext.layoutConfig);

    const toggleLeftSidebar = () => {
        setLayoutConfig({
            ...layoutConfig,
            leftSidebarOpen: !layoutConfig.leftSidebarOpen
        });
    };

    return (
        <LayoutContext.Provider value={{
            layoutConfig,
            setLayoutConfig,
            toggleLeftSidebar
        }}>
            {props.children}
        </LayoutContext.Provider>
    );
}
