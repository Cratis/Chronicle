// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Nav, INavLinkGroup, INavLink, INavStyles } from '@fluentui/react';
import { useNavigate } from 'react-router-dom';

import { default as styles } from './Navigation.module.scss';

const navStyles: Partial<INavStyles> = {
    root: {
        width: 158
    },
    link: {
        whiteSpace: 'normal',
        lineHeight: 'inherit',
    },
};

const groups: INavLinkGroup[] = [
    {
        links: [
            {
                name: 'Home',
                url: '',
                route: '/'
            },
            {
                name: 'Microservices',
                url: '',
                route: '/microservices'
            },
            {
                name: 'GDPR',
                url: '',
                links: [
                    {
                        name: 'People',
                        key: 'people',
                        url: '',
                        route: '/gdpr/people'
                    }
                ]
            },
            {
                name: 'Events',
                url: '',
                links: [
                    {
                        name: 'Types',
                        key: 'types',
                        url: '',
                        route: '/events/types'
                    },
                    {
                        name: 'EventLog',
                        key: 'event-log',
                        url: '',
                        route: '/events/eventlogs'
                    },
                    {
                        name: "Projections",
                        key: 'projections',
                        url: '',
                        route: '/events/projections'
                    }
                ]
            }
        ]
    }
];

export const Navigation = () => {
    const [selectedNav, setSelectedNav] = useState('');
    const history = useNavigate();

    const navItemClicked = (ev?: React.MouseEvent<HTMLElement>, item?: INavLink) => {
        if (item) {
            setSelectedNav(item.key!);
            history(item.route);
        }
    };
    return (
        <div className={styles.navigationContainer}>
            <Nav
                groups={groups}
                styles={navStyles}
                onLinkClick={navItemClicked}
                selectedKey={selectedNav} />
        </div>
    );
};
