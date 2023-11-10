import { useColorScheme } from './Utils/useColorScheme';
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { BlankLayout } from "./Layout/Blank/BlankLayout";
import { Home } from "./Components/Pages/Home";
import { EventStore } from "./Components/Pages/EventStore/EventStore";

function App() {
    useColorScheme();
    return (
        <BrowserRouter>
            <Routes>
                <Route path='/' element={<Navigate to={'/home'}/>}/>
                <Route path='/home' element={<BlankLayout/>}>
                    <Route path={''} element={<Home/>}/>
                </Route>
                <Route path='/event-store/*' element={<EventStore/>}>
                </Route>
            </Routes>
        </BrowserRouter>
    );
}

export default App;