import { forwardRef } from 'react';
import { ThemeSwitch } from './ThemeSwitch';
import classes from './Topbar.module.css';
import { FaHamburger } from "react-icons/fa";
import { Button } from "primereact/button";
import { useLayoutContext } from "../context/LayoutContext";

export interface AppTopbarRef {
    menubutton?: HTMLButtonElement | null;
    topbarmenu?: HTMLDivElement | null;
    topbarmenubutton?: HTMLButtonElement | null;
}

export const Topbar = forwardRef<AppTopbarRef>(() => {
    const { toggleLeftSidebarOpen, layoutConfig } = useLayoutContext();

    return <div className={classes.container}>
        {!layoutConfig.leftSidebarHidden &&
            <Button icon={FaHamburger} onClick={toggleLeftSidebarOpen} className="p-button-rounded p-button-text"/>}
        <ThemeSwitch/>
    </div>;
});
