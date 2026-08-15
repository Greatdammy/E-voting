export default function OtpInput({ value, onChange, disabled = false, length = 6, id }) {
  const handleChange = (event) => {
    const digitsOnly = event.target.value.replace(/\D/g, '').slice(0, length);
    onChange(digitsOnly);
  };

  return (
    <input
      id={id}
      type="text"
      inputMode="numeric"
      autoComplete="one-time-code"
      pattern="\d*"
      maxLength={length}
      value={value}
      onChange={handleChange}
      disabled={disabled}
      placeholder={'•'.repeat(length)}
      aria-label="Verification code"
      className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-center text-2xl font-mono tracking-[0.5em] text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100"
    />
  );
}