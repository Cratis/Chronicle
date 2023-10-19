import { forwardRef } from 'react';
import { ThemeSwitch } from './ThemeSwitch';
import classes from './Topbar.module.css';

export interface AppTopbarRef {
    menubutton?: HTMLButtonElement | null;
    topbarmenu?: HTMLDivElement | null;
    topbarmenubutton?: HTMLButtonElement | null;
}

export const Topbar = forwardRef<AppTopbarRef>(() => {
    return <div className={classes.container}>
        <ThemeSwitch/>
    </div>;
});
