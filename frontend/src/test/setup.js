import '@testing-library/jest-dom/vitest';

// jsdom doesn't implement ResizeObserver; recharts' ResponsiveContainer
// needs it to mount at all.
if (typeof globalThis.ResizeObserver === 'undefined') {
  globalThis.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
}
