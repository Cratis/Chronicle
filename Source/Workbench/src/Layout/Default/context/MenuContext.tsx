import { useState, createContext, Dispatch, SetStateAction, ReactNode } from 'react';

export interface MenuContextProps {
    activeMenu: string;
    setActiveMenu: Dispatch<SetStateAction<string>>;
}

export interface MenuProviderProps {
    children: ReactNode;
}

export const MenuContext = createContext({} as MenuContextProps);

export const MenuProvider = ({ children }: MenuProviderProps) => {
    const [activeMenu, setActiveMenu] = useState('');

    const value = {
        activeMenu,
        setActiveMenu,
    };

    return <MenuContext.Provider value={value}>{children}</MenuContext.Provider>;
};
