import { useLayoutContext } from "../context/LayoutContext";
import { ThemeSwitch } from './ThemeSwitch';
import { Button } from "primereact/button";
import classes from './Topbar.module.css';
import { FaBars } from "react-icons/fa6";
import { forwardRef } from 'react';

export interface AppTopbarRef {
    menubutton?: HTMLButtonElement | null;
    topbarmenu?: HTMLDivElement | null;
    topbarmenubutton?: HTMLButtonElement | null;
}

export const Topbar = forwardRef<AppTopbarRef>(() => {
    const { toggleLeftSidebarOpen, layoutConfig } = useLayoutContext();

    return <div className={classes.container + ' px-4'}>
        {!layoutConfig.leftSidebarHidden &&
            <Button onClick={toggleLeftSidebarOpen} className="p-button-rounded p-button-text p-2">
                <FaBars/>
            </Button>
            }
        <ThemeSwitch/>
    </div>;
});
