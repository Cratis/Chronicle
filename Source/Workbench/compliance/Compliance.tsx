// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NavigationPage, NavigationButton } from '../Components/Navigation';
import * as icons from '@mui/icons-material';

export const Compliance = () => {
    return (
        <NavigationPage>
            <NavigationPage.Navigation>
                <>
                    <NavigationButton
                        title="GDPR"
                        icon={<icons.HowToReg/>}
                        />
                </>

            </NavigationPage.Navigation>

        </NavigationPage>
    );
};
