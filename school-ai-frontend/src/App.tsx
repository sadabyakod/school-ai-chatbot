import LandingPage from './pages/LandingPage';
import StudentDashboard from './pages/StudentDashboard';
import TeacherDashboard from './pages/TeacherDashboard';
import { ToastContainer } from './components/Toast';
import { useToast } from './hooks/useToast';
import { useState } from "react";

type UserRole = 'student' | 'teacher' | null;

function App() {
  const [userRole, setUserRole] = useState<UserRole>(null);
  const toast = useToast();

  const handleRoleSelection = (role: 'student' | 'teacher') => {
    setUserRole(role);
  };

  const handleLogout = () => {
    setUserRole(null);
    localStorage.removeItem('jwt');
  };

  return (
    <>
      <ToastContainer toasts={toast.toasts} onClose={toast.removeToast} />
      {!userRole && <LandingPage onSelectRole={handleRoleSelection} />}
      {userRole === 'student' && <StudentDashboard onLogout={handleLogout} />}
      {userRole === 'teacher' && <TeacherDashboard onLogout={handleLogout} />}
    </>
  );
}

export default App;
