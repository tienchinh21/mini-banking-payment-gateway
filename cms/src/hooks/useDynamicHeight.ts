import { useEffect, useState, type RefObject } from 'react'

interface DynamicHeightOptions {
  offsetBottom?: number // Extra offset from bottom (e.g. pagination + padding)
  minHeight?: number    // Minimum height threshold
  defaultHeight?: number
}

/**
 * Automatically calculate remaining scroll height for Table based on element position
 */
export function useDynamicHeight(
  containerRef?: RefObject<HTMLElement | null>,
  options: DynamicHeightOptions = {}
): number {
  const { offsetBottom = 160, minHeight = 280, defaultHeight = 480 } = options
  const [height, setHeight] = useState<number>(defaultHeight)

  useEffect(() => {
    const calculateHeight = () => {
      if (containerRef?.current) {
        const rect = containerRef.current.getBoundingClientRect()
        const availableHeight = window.innerHeight - rect.top - offsetBottom
        setHeight(Math.max(minHeight, availableHeight))
      } else {
        // Fallback to window height minus typical header/filter/footer offset
        const availableHeight = window.innerHeight - 300
        setHeight(Math.max(minHeight, availableHeight))
      }
    }

    calculateHeight()

    window.addEventListener('resize', calculateHeight)
    return () => {
      window.removeEventListener('resize', calculateHeight)
    }
  }, [containerRef, offsetBottom, minHeight])

  return height
}
