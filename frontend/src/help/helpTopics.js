// Plain keyword-matched FAQ — no LLM, no ML, no network call. Deliberately
// NOT styled/branded as "AI" (see HelpWidget.jsx) so the app's real AI
// claims (Integrity Guard, Turnout Forecast) stay unambiguous.
//
// Matching is scored, not first-match: every topic's keyword phrases are
// checked, and whichever topic has the most matches wins (ties go to
// whichever is listed first). This matters because a first-match approach
// means a topic's ranking in this array silently determines what a
// borderline question gets answered with — scoring instead means a message
// that happens to brush past an early topic's one weak keyword doesn't
// steal the match from a topic it's actually about.
const topics = [
  {
    keywords: [
      'register',
      'sign up',
      'signup',
      'create account',
      'create an account',
      'new account',
      'make an account',
      'get started',
      'join'
    ],
    response:
      "To register, go to the Register page and enter your full name, email, and a password. You'll be signed up as a Voter — no separate verification step is needed before you can log in."
  },
  {
    keywords: [
      'log in',
      'login',
      'sign in',
      'signin',
      "can't log in",
      'cant log in',
      'password',
      'forgot my password',
      'access my account',
      'session expired',
      'logged out'
    ],
    response:
      "Log in with the email and password you registered with. If your session expires, you'll be redirected to the Login page automatically. There's no password-reset flow yet — an administrator would need to help if you're locked out."
  },
  {
    keywords: [
      'vote',
      'voting',
      'ballot',
      'cast',
      'how do i vote',
      'how to vote',
      'choose a candidate',
      'select a candidate',
      'submit my vote',
      'already voted',
      'voted already',
      'can i vote again',
      'change my vote'
    ],
    response:
      "To vote: go to Elections, find one marked Active, click Vote to open its ballot, select a candidate, and submit. You'll get a confirmation receipt afterward — you can only vote once per election, and votes can't be changed after submitting."
  },
  {
    keywords: [
      'upcoming',
      'active',
      'closed',
      'status',
      'when does it start',
      'when does it end',
      'is it open',
      'can i still vote',
      'election over',
      'has the election started',
      'has the election ended'
    ],
    response:
      "Upcoming means the election hasn't started yet. Active means voting is open right now. Closed means voting has ended and the tally is final. You can only cast a vote while an election is Active."
  },
  {
    keywords: [
      'receipt',
      'confirmation',
      'hash',
      'confirmation hash',
      'proof',
      'verify my vote',
      'did my vote count',
      'prove i voted'
    ],
    response:
      "After you vote, you get a confirmation hash — a code that proves your vote was recorded without revealing who you voted for. It's verifiable, but it can't be used by anyone (including admins) to look up your choice."
  },
  {
    keywords: [
      'results',
      'live',
      'tally',
      'count',
      'who is winning',
      'how many votes',
      'see results',
      'view results',
      'check the results'
    ],
    response:
      "Results pages update live as votes come in while an election is Active, and show the final tally once it's Closed. You don't need to refresh the page."
  },
  {
    keywords: ['dark mode', 'theme', 'light mode', 'dark theme', 'switch theme', 'night mode'],
    response: 'Use the sun/moon icon in the navigation bar to switch between light and dark mode. Your choice is remembered on this device.'
  },
  {
    keywords: [
      'admin',
      'administrator',
      'election officer',
      'officer',
      'who can create',
      'who manages',
      'manage candidates',
      'delete an election',
      'delete election'
    ],
    response:
      'Administrators and Election Officers can create elections and manage candidates. Only Administrators can create other admin/officer accounts or delete an election (and only if it has no votes cast yet).'
  },
  {
    keywords: ['hello', 'hi', 'hey', 'help', 'what can you do', 'what do you know', 'what can you help with'],
    response:
      "Hi! Ask me about registering, logging in, voting, election statuses, your confirmation receipt, live results, dark mode, or admin/officer roles."
  }
];

const fallback =
  "Sorry, I didn't understand that. I can help with: registering, logging in, voting, what election statuses mean, your confirmation receipt, live results, dark mode, and admin/officer roles.";

function normalize(message) {
  return message.toLowerCase().replace(/[^a-z0-9\s]/g, ' ');
}

export function findAnswer(message) {
  const normalized = normalize(message);

  let best = null;
  let bestScore = 0;

  for (const topic of topics) {
    // Keywords are normalized the same way as the message (not pre-stored
    // normalized) so both sides go through one identical transform — e.g.
    // "can't log in" and a typed "cant log in" must end up as the same
    // string, or stripping punctuation from only one side silently breaks
    // every keyword that contains an apostrophe.
    const score = topic.keywords.reduce(
      (count, keyword) => (normalized.includes(normalize(keyword)) ? count + 1 : count),
      0
    );
    if (score > bestScore) {
      bestScore = score;
      best = topic;
    }
  }

  return best ? best.response : fallback;
}
