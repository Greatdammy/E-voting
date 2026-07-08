import { createSlice } from '@reduxjs/toolkit';

const STORAGE_KEY = 'evoting_auth';

const emptyAuth = { token: null, userId: null, role: null, expiresAt: null };

function loadInitialState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? { ...emptyAuth, ...JSON.parse(raw) } : emptyAuth;
  } catch {
    return emptyAuth;
  }
}

const authSlice = createSlice({
  name: 'auth',
  initialState: loadInitialState(),
  reducers: {
    setCredentials(state, action) {
      const { token, userId, role, expiresAt } = action.payload;
      state.token = token;
      state.userId = userId;
      state.role = role;
      state.expiresAt = expiresAt;
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ token, userId, role, expiresAt }));
    },
    logout(state) {
      state.token = null;
      state.userId = null;
      state.role = null;
      state.expiresAt = null;
      localStorage.removeItem(STORAGE_KEY);
    }
  }
});

export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;
