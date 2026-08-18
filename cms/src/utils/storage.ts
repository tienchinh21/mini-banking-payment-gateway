/**
 * Safe local storage wrapper
 */
export const storage = {
  get<T>(key: string, defaultValue?: T): T | null {
    try {
      const item = localStorage.getItem(key)
      if (item === null) return defaultValue ?? null
      return JSON.parse(item) as T
    } catch {
      return defaultValue ?? null
    }
  },

  set<T>(key: string, value: T): void {
    try {
      localStorage.setItem(key, JSON.stringify(value))
    } catch (e) {
      console.error(`Error saving to localStorage key "${key}"`, e)
    }
  },

  remove(key: string): void {
    try {
      localStorage.removeItem(key)
    } catch (e) {
      console.error(`Error removing localStorage key "${key}"`, e)
    }
  },

  clear(): void {
    try {
      localStorage.clear()
    } catch (e) {
      console.error('Error clearing localStorage', e)
    }
  },
}
