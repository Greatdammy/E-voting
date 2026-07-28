import { useState } from 'react';
import { HelpCircle, Send, X } from 'lucide-react';
import { findAnswer } from '../help/helpTopics';
import Card from './ui/Card';
import Button from './ui/Button';

const introMessage = {
  role: 'bot',
  text: "Hi! I can answer quick questions about registering, voting, election statuses, receipts, results, and dark mode."
};

export default function HelpWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([introMessage]);
  const [draft, setDraft] = useState('');

  const handleSend = (event) => {
    event.preventDefault();
    const text = draft.trim();
    if (!text) {
      return;
    }

    const reply = findAnswer(text);
    setMessages((prev) => [...prev, { role: 'user', text }, { role: 'bot', text: reply }]);
    setDraft('');
  };

  return (
    <div className="fixed bottom-4 right-4 z-20">
      {open && (
        <Card className="mb-3 flex h-96 w-80 flex-col overflow-hidden p-0 shadow-xl">
          <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
            <span className="flex items-center gap-1.5 text-sm font-semibold text-slate-900 dark:text-white">
              <HelpCircle className="h-4 w-4" />
              Help
            </span>
            <button
              type="button"
              onClick={() => setOpen(false)}
              aria-label="Close help"
              className="rounded-lg p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          <div className="flex-1 space-y-2 overflow-y-auto px-4 py-3">
            {messages.map((message, index) => (
              <div
                key={index}
                className={`max-w-[85%] rounded-lg px-3 py-2 text-sm ${
                  message.role === 'user'
                    ? 'ml-auto bg-indigo-600 text-white'
                    : 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300'
                }`}
              >
                {message.text}
              </div>
            ))}
          </div>

          <form onSubmit={handleSend} className="flex gap-2 border-t border-slate-200 p-3 dark:border-slate-800">
            <input
              type="text"
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              placeholder="Ask a question..."
              className="flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            />
            <Button type="submit" aria-label="Send">
              <Send className="h-4 w-4" />
            </Button>
          </form>
        </Card>
      )}

      <Button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        aria-label={open ? 'Close help' : 'Open help'}
        className="ml-auto flex h-12 w-12 items-center justify-center rounded-full p-0 shadow-lg"
      >
        {open ? <X className="h-5 w-5" /> : <HelpCircle className="h-5 w-5" />}
      </Button>
    </div>
  );
}
