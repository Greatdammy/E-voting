export function extractErrorMessage(error, fallback) {
  const data = error?.response?.data;
  if (!data) {
    return fallback;
  }

  if (data.message) {
    return data.message;
  }

  if (data.errors) {
    const messages = Object.values(data.errors).flat();
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  return fallback;
}
