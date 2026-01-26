// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutContext } from '../context/LayoutContext';
import { Button } from 'primereact/button';
import css from './TopBar.module.css';
import { FaBars } from 'react-icons/fa6';
import { version } from '../../../version';
// import { Profile } from "./Profile";
// import { Notifications } from './Notifications';
// import { Connection } from './Connection';
import { Breadcrumb } from './Breadcrumb';

export const TopBar = () => {
    const { toggleLeftSidebarOpen } = useLayoutContext();

    return (
        <div className={css.container}>
            <div className={`flex items-center ${css.leftSide}`}>
                <div className={css.sidebarToggle}>
                    <Button
                        onClick={toggleLeftSidebarOpen}
                        text
                        className={css.hamburgerMenuButton}>
                        <FaBars />
                    </Button>
                </div>
                <div className={css.breadcrumbContainer}>
                    <Breadcrumb />
                </div>
            </div>
            <div className="flex-1 flex items-center justify-end px-5 gap-6">
                <div className={css.versionInfo}>
                    <div className={css.version}>v{version.version}</div>
                    <div className={css.commitSha}>{version.commitSha.length >= 7 ? version.commitSha.substring(0, 7) : version.commitSha}</div>
                </div>
                {/* <div>
                    <Connection />
                </div>
                <div>
                    <Notifications />
                </div>
                <div>
                    <Profile />
                </div> */}
            </div>
        </div>
    );
};

