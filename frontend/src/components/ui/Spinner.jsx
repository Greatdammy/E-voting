import { Loader2 } from 'lucide-react';

export default function Spinner({ className = '', label = 'Loading...' }) {
  return (
    <div className={`flex items-center justify-center gap-2 py-8 text-slate-500 dark:text-slate-400 ${className}`}>
      <Loader2 className="h-5 w-5 animate-spin" />
      <span className="text-sm">{label}</span>
    </div>
  );
}
