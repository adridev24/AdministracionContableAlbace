// Calendar dates preserve the written day, regardless of the browser time zone.
export const calendarDateValue = (value) => {
  if (typeof value !== 'string') return '';
  const match = /^(\d{4})-(\d{2})-(\d{2})(?:T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?(?:Z|[+-](?:[01]\d|2[0-3]):[0-5]\d)?)?$/.exec(value);
  if (!match) return '';
  const [, year, month, day] = match;
  const y = Number(year), m = Number(month), d = Number(day);
  const leap = y % 4 === 0 && (y % 100 !== 0 || y % 400 === 0);
  const days = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
  return y >= 1 && m >= 1 && m <= 12 && d >= 1 && d <= days[m - 1]
    ? `${year}-${month}-${day}` : '';
};

export const formatCalendarDate = (value) => {
  const date = calendarDateValue(value);
  return date ? date.split('-').reverse().join('/') : '-';
};

// Keep the existing DateTime/timestamptz API contract at UTC midnight.
export const calendarDateToApi = (value) => {
  const date = calendarDateValue(value);
  return date ? `${date}T00:00:00.000Z` : null;
};
