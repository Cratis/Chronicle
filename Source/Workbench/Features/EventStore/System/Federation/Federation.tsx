// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { Page } from 'Components/Common/Page';
import { Tabs, TabPanel } from 'Components/Tabs';
import { ConsumerBindings } from './Bindings/ConsumerBindings';
import { ProducerGrants } from './Grants/ProducerGrants';

export const Federation = () => (
    <Page title={strings.federation.title}>
        <Tabs>
            <TabPanel header={strings.federation.bindings.tab}>
                <ConsumerBindings />
            </TabPanel>
            <TabPanel header={strings.federation.grants.tab}>
                <ProducerGrants />
            </TabPanel>
        </Tabs>
    </Page>
);
