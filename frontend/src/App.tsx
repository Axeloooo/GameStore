import { Outlet } from 'react-router-dom';
import './App.css';
import NavMenu from './components/NavMenu';

function App() {
  return (
    <div>
      <NavMenu />
      <div className="container">
        <Outlet />
      </div>
    </div>
  );
}

export default App;