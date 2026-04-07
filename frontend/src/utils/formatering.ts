/**
 * Formaterer et belop i norsk format (1 234,56).
 * Negative belop vises i rodt med parentes: (1 234,56).
 */
export function formatBelop(verdi: number): string {
  if (verdi < 0) {
    return `(${Math.abs(verdi).toLocaleString('nb-NO', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })})`;
  }
  return verdi.toLocaleString('nb-NO', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

/**
 * Formaterer en dato i norsk format: dd.MM.yyyy.
 */
export function formatDato(dato: string | Date): string {
  const d = typeof dato === 'string' ? new Date(dato) : dato;
  return d.toLocaleDateString('nb-NO', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

/**
 * Formaterer et kontonummer med mellomrom for lesbarhet.
 * 4-sifret: "1920" -> "1920"
 * 5-sifret: "19201" -> "1920.1"
 * 6-sifret: "192010" -> "1920.10"
 */
export function formatKontonummer(kontonummer: string): string {
  if (kontonummer.length <= 4) {
    return kontonummer;
  }
  return `${kontonummer.slice(0, 4)}.${kontonummer.slice(4)}`;
}

/**
 * Parser et norsk formatert belop tilbake til tall.
 * Handterer "1 234,56" -> 1234.56
 */
export function parseBelop(tekst: string): number | null {
  // Fjern parenteser (negativt belop)
  const erNegativ = tekst.startsWith('(') && tekst.endsWith(')');
  let renset = tekst.replace(/[()]/g, '');

  // Fjern tusenskilletegn (mellomrom og non-breaking space)
  renset = renset.replace(/[\s\u00a0]/g, '');

  // Bytt komma til punktum
  renset = renset.replace(',', '.');

  const tall = parseFloat(renset);
  if (isNaN(tall)) return null;

  return erNegativ ? -tall : tall;
}

/**
 * Formaterer en MVA-sats med prosenttegn.
 */
export function formatMvaSats(sats: number): string {
  return `${sats.toLocaleString('nb-NO', { minimumFractionDigits: 0, maximumFractionDigits: 2 })} %`;
}
