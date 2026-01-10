// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useWorkbenchContext } from '../context/WorkbenchContext';
import * as Shared from 'Shared';
import { FaHouse } from 'react-icons/fa6';
import css from './Breadcrumb.module.css';
import { useEffect } from 'react';

export const Breadcrumb = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const params = useParams<Shared.EventStoreAndNamespaceParams>();
    const { pageTitle, setPageTitle } = useWorkbenchContext();

    const navigateToHome = () => {
        navigate('/');
    };

    const navigateToEventStore = () => {
        if (params.eventStore) {
            navigate(`/event-store/${params.eventStore}`);
        }
    };

    // Clear page title when at event store root
    useEffect(() => {
        const isEventStoreRoot = location.pathname === `/event-store/${params.eventStore}` || 
                                 location.pathname === `/event-store/${params.eventStore}/`;
        if (isEventStoreRoot) {
            setPageTitle('');
        }
    }, [location.pathname, params.eventStore, setPageTitle]);

    return (
        <div className={css.breadcrumb}>
            <a
                href="#"
                onClick={(e) => {
                    e.preventDefault();
                    navigateToHome();
                }}
                className={css.home}>
                <FaHouse />
            </a>
            {params.eventStore && (
                <>
                    <span className={css.separator}>/</span>
                    <a
                        href="#"
                        onClick={(e) => {
                            e.preventDefault();
                            navigateToEventStore();
                        }}
                        className={css.eventStore}>
                        {params.eventStore}
                    </a>
                    {pageTitle && (
                        <>
                            <span className={css.separator}>/</span>
                            <span className={css.pageTitle}>{pageTitle}</span>
                        </>
                    )}
                </>
            )}
        </div>
    );
};
