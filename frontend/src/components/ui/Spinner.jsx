import { Loader2, Sparkles } from 'lucide-react';

// The "ai" variant is a distinct inline indicator — fuchsia (the shared AI
// accent, see TurnoutForecastCard/AdminIntegrityPage) + a pulsing Sparkles
// glyph instead of the neutral spinner — so a "waiting on an AI signal"
// state never looks identical to an ordinary network-loading state.
export default function Spinner({ className = '', label = 'Loading...', variant = 'default' }) {
  if (variant === 'ai') {
    return (
      <div className={`flex items-center gap-2 text-fuchsia-600 dark:text-fuchsia-400 ${className}`}>
        <Sparkles className="h-4 w-4 animate-pulse" />
        <span className="text-sm">{label}</span>
      </div>
    );
  }

  return (
    <div className={`flex items-center justify-center gap-2 py-8 text-slate-500 dark:text-slate-400 ${className}`}>
      <Loader2 className="h-5 w-5 animate-spin" />
      <span className="text-sm">{label}</span>
    </div>
  );
}
