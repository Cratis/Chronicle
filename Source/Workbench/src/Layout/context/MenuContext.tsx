import { MenuContextProps, ChildContainerProps } from '../layout';
import { useState, createContext } from 'react';

export const MenuContext = createContext({} as MenuContextProps);

export const MenuProvider = ({ children }: ChildContainerProps) => {
    const [activeMenu, setActiveMenu] = useState('');

    const value = {
        activeMenu,
        setActiveMenu,
    };

    return <MenuContext.Provider value={value}>{children}</MenuContext.Provider>;
};
