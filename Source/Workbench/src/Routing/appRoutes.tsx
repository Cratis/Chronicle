import { Opensjon } from '../Components/Pages/Opensjon';
import { RouteDefinition } from './RouteDefinition';
import { postLoader } from '../TestData/loader';
import { Test } from '../TestData/Test';
import App from '../App';

export const appRoutes: RouteDefinition[] = [
    {
        title: 'Home',
        path: '/',
        element: <App />,
    },
    {
        title: 'Test',
        path: '/test',
        loader: postLoader,
        element: <Test />,
        errorElement: <>This is not working</>,
    },
    {
        title: 'Opensjon',
        path: '/opensjon',
        element: <Opensjon />,
        errorElement: <>This is not working</>,
    },
];
