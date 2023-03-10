// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NavigationRootItem } from '../Components/Navigation/NavigationRootItem';
import { NavigationPage } from '../Components/Navigation/NavigationPage';
import * as icons from '@mui/icons-material';
import { useNavigate, Routes, Route } from 'react-router-dom';
import { Microservices } from './Microservices';
import { Tenants } from './Tenants';

export const Configuration = () => {
    const navigate = useNavigate();

    return (
        <NavigationPage>
            <NavigationPage.Navigation>
                <NavigationRootItem
                    title="Tenants"
                    icon={<icons.Apartment />}
                    onClick={() => navigate('/configuration/tenants')} />

                <NavigationRootItem
                    title="Microservices"
                    icon={<icons.Cabin />}
                    onClick={() => navigate('/configuration/microservices')} />

            </NavigationPage.Navigation>
            <NavigationPage.Content>
                <Routes>
                    <Route path="microservices" element={<Microservices />} />
                    <Route path="tenants" element={<Tenants />} />
                </Routes>
            </NavigationPage.Content>
        </NavigationPage>
    );
};
