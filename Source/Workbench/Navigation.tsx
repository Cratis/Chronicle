// Copyright (c) Aksio Insurtech. All rights reserved.
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
                name: 'Configuration',
                url: '',
                links: [
                    {
                        name: 'Microservices',
                        url: '',
                        route: '/configuration/microservices'
                    },
                    {
                        name: 'Tenants',
                        url: '',
                        route: '/configuration/tenants'
                    }
                ]
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
                name: 'Event Store',
                url: '',
                links: [
                    {
                        name: 'Types',
                        key: 'types',
                        url: '',
                        route: '/events/store/types'
                    },
                    {
                        name: 'EventSequence',
                        key: 'event-sequence',
                        url: '',
                        route: '/events/store/sequence'
                    },
                    {
                        name: 'Observers',
                        key: 'observers',
                        url: '',
                        route: '/events/store/observers'
                    },
                    {
                        name: "Projections",
                        key: 'projections',
                        url: '',
                        route: '/events/store/projections'
                    }
                ]
            }
        ]
    }
];

export const Navigation = () => {
    const [selectedNav, setSelectedNav] = useState('');
    const navigate = useNavigate();

    const navItemClicked = (ev?: React.MouseEvent<HTMLElement>, item?: INavLink) => {
        if (item) {
            ev?.preventDefault();
            setSelectedNav(item.key!);
            navigate(item.route);
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
