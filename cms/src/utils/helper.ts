/**
 * Generate unique correlation ID
 */
export function generateCorrelationId(): string {
  return `cms-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`
}

/**
 * Clean empty/null/undefined params from query object
 */
export function cleanParams<T extends Record<string, any>>(obj: T): Partial<T> {
  const result: Record<string, any> = {}
  Object.keys(obj).forEach((key) => {
    const value = obj[key]
    if (value !== undefined && value !== null && value !== '') {
      result[key] = value
    }
  })
  return result as Partial<T>
}

/**
 * Copy text to clipboard
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text)
      return true
    }
    const textArea = document.createElement('textarea')
    textArea.value = text
    textArea.style.position = 'fixed'
    textArea.style.left = '-999999px'
    document.body.appendChild(textArea)
    textArea.focus()
    textArea.select()
    const successful = document.execCommand('copy')
    textArea.remove()
    return successful
  } catch {
    return false
  }
}
