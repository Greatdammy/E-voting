import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { LogOut, Menu, Moon, ShieldCheck, Sparkles, Sun, UserPlus, Vote, X } from 'lucide-react';
import { logout } from '../store/authSlice';
import { useDarkMode } from '../hooks/useDarkMode';

export default function NavBar() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { token, role } = useSelector((state) => state.auth);
  const { theme, toggleTheme } = useDarkMode();
  const [menuOpen, setMenuOpen] = useState(false);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
    setMenuOpen(false);
  };

  const links = [];
  if (token && role === 'Voter') {
    links.push({ to: '/elections', label: 'Elections', icon: Vote });
  }
  if (token && (role === 'Administrator' || role === 'ElectionOfficer')) {
    links.push({ to: '/admin/elections', label: 'Admin', icon: ShieldCheck });
  }
  if (token && role === 'Administrator') {
    links.push({ to: '/admin/users', label: 'Create User', icon: UserPlus });
  }

  const linkClass =
    'flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800';

  return (
    <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/80 backdrop-blur dark:border-slate-800 dark:bg-slate-950/80">
      <nav className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3 sm:px-6">
        <Link to="/" className="flex items-center gap-2 text-lg font-semibold text-slate-900 dark:text-white">
          <span className="relative flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-indigo-600 to-violet-600 text-white">
            <Vote className="h-4 w-4" />
            {/* Permanent, subtle cue that the platform has AI-assisted
                features — fuchsia (the shared AI accent) so it never reads
                as part of the indigo/violet brand mark itself. */}
            <Sparkles
              className="absolute -right-1.5 -top-1.5 h-3.5 w-3.5 rounded-full bg-white text-fuchsia-600 dark:bg-slate-950 dark:text-fuchsia-400"
              aria-hidden="true"
            />
          </span>
          E-Voting
        </Link>

        <div className="hidden items-center gap-1 sm:flex">
          {!token && (
            <>
              <Link to="/login" className={linkClass}>
                Login
              </Link>
              <Link
                to="/register"
                className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500"
              >
                Register
              </Link>
            </>
          )}
          {links.map(({ to, label, icon: Icon }) => (
            <Link key={to} to={to} className={linkClass}>
              <Icon className="h-4 w-4" />
              {label}
            </Link>
          ))}
          <button
            type="button"
            onClick={toggleTheme}
            aria-label="Toggle dark mode"
            className="ml-1 rounded-lg p-2 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
          >
            {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </button>
          {token && (
            <button
              type="button"
              onClick={handleLogout}
              className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-rose-600 hover:bg-rose-50 dark:text-rose-400 dark:hover:bg-rose-500/10"
            >
              <LogOut className="h-4 w-4" />
              Logout
            </button>
          )}
        </div>

        <button
          type="button"
          className="rounded-lg p-2 text-slate-600 dark:text-slate-300 sm:hidden"
          onClick={() => setMenuOpen((open) => !open)}
          aria-label="Toggle menu"
        >
          {menuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </nav>

      {menuOpen && (
        <div className="border-t border-slate-200 px-4 py-3 dark:border-slate-800 sm:hidden">
          <div className="flex flex-col gap-1">
            {!token && (
              <>
                <Link to="/login" onClick={() => setMenuOpen(false)} className={linkClass}>
                  Login
                </Link>
                <Link to="/register" onClick={() => setMenuOpen(false)} className={linkClass}>
                  Register
                </Link>
              </>
            )}
            {links.map(({ to, label, icon: Icon }) => (
              <Link key={to} to={to} onClick={() => setMenuOpen(false)} className={linkClass}>
                <Icon className="h-4 w-4" />
                {label}
              </Link>
            ))}
            <button type="button" onClick={toggleTheme} className={`${linkClass} text-left`}>
              {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
              Toggle theme
            </button>
            {token && (
              <button
                type="button"
                onClick={handleLogout}
                className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-left text-sm font-medium text-rose-600 hover:bg-rose-50 dark:text-rose-400 dark:hover:bg-rose-500/10"
              >
                <LogOut className="h-4 w-4" />
                Logout
              </button>
            )}
          </div>
        </div>
      )}
    </header>
  );
}
