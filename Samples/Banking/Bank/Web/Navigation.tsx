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
                name: 'Accounts',
                url: '',
                links: [
                    {
                        name: 'Debit',
                        key: 'debit',
                        url: '',
                        route: '/accounts/debit'
                    }
                ]
            },
            {
                name: 'Integration',
                url: '',
                route: '/integration'
            },

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
