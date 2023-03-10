// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NavigationHeader } from '../Components/Navigation/NavigationHeader';
import { NavigationPage } from '../Components/Navigation/NavigationPage';
import * as icons from '@mui/icons-material';

export const Configuration = () => {
    return (
        <NavigationPage>
            <NavigationPage.Navigation>
                <NavigationHeader title="Event Store">
                    <NavigationHeader.Icon>
                        <icons.Storage />
                    </NavigationHeader.Icon>
                </NavigationHeader>
            </NavigationPage.Navigation>
            <NavigationPage.Content>
                <div>
                    Hello
                </div>
            </NavigationPage.Content>
        </NavigationPage>
    );
};
