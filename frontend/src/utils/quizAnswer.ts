import { parseAnswerToArray } from '@/utils/answerFormatter'

export type ChoiceAnswerMode = 'key' | 'text'

const OPTION_KEY_PATTERN = /^([A-Za-z])(?:[.)、\s:：-].*)?$/
const OPTION_PREFIX_PATTERN = /^[A-Za-z][.)、\s:：-]+/

function normalizeToken(value: string): string {
  return value.trim()
}

function stripOptionPrefix(value: string): string {
  return value.replace(OPTION_PREFIX_PATTERN, '').trim()
}

export function getOptionKey(index: number): string {
  return String.fromCharCode(65 + index)
}

export function extractOptionKey(value: string): string | null {
  const match = value.trim().match(OPTION_KEY_PATTERN)
  if (!match || !match[1]) {
    return null
  }
  return match[1].toUpperCase()
}

export function resolveOptionIndex(value: string, options: string[]): number | null {
  const token = normalizeToken(value)
  if (!token) {
    return null
  }

  const key = extractOptionKey(token)
  if (key) {
    const keyIndex = key.charCodeAt(0) - 65
    if (keyIndex >= 0 && keyIndex < options.length) {
      return keyIndex
    }
  }

  const exactIndex = options.findIndex(
    option => option.trim().toLowerCase() === token.toLowerCase()
  )
  if (exactIndex >= 0) {
    return exactIndex
  }

  const strippedToken = stripOptionPrefix(token).toLowerCase()
  const strippedIndex = options.findIndex(
    option => stripOptionPrefix(option).toLowerCase() === strippedToken
  )
  return strippedIndex >= 0 ? strippedIndex : null
}

export function inferChoiceAnswerMode(
  correctAnswer: string | string[] | undefined,
  options: string[]
): ChoiceAnswerMode {
  const tokens = parseAnswerToArray(correctAnswer).map(normalizeToken).filter(Boolean)
  if (tokens.length === 0) {
    return 'text'
  }

  const allAsKeys = tokens.every(token => {
    const key = extractOptionKey(token)
    if (!key) {
      return false
    }
    const index = key.charCodeAt(0) - 65
    return index >= 0 && index < options.length
  })

  return allAsKeys ? 'key' : 'text'
}

export function mapChoiceAnswerForSubmit(
  answer: string | string[],
  options: string[],
  mode: ChoiceAnswerMode,
  multiple: boolean
): string | string[] {
  const tokens = parseAnswerToArray(answer).map(normalizeToken).filter(Boolean)
  const mapped = tokens
    .map(token => {
      const index = resolveOptionIndex(token, options)
      if (mode === 'key') {
        if (index !== null) {
          return getOptionKey(index)
        }
        const key = extractOptionKey(token)
        return key ?? token
      }

      if (index !== null) {
        return options[index] || token
      }
      return token
    })
    .filter(Boolean)

  const unique = Array.from(new Set(mapped))
  return multiple ? unique : (unique[0] ?? '')
}

export function mapChoiceAnswerForDisplay(
  answer: string | string[] | undefined,
  options: string[],
  multiple: boolean
): string | string[] {
  const tokens = parseAnswerToArray(answer).map(normalizeToken).filter(Boolean)
  const mapped = tokens
    .map(token => {
      const index = resolveOptionIndex(token, options)
      if (index !== null) {
        return options[index] || token
      }
      return token
    })
    .filter(Boolean)

  const unique = Array.from(new Set(mapped))
  return multiple ? unique : (unique[0] ?? '')
}

export function compareChoiceAnswers(
  userAnswer: string | string[] | undefined,
  correctAnswer: string | string[] | undefined,
  options: string[]
): boolean {
  const userIndexes = parseAnswerToArray(userAnswer)
    .map(token => resolveOptionIndex(token, options))
    .filter((index): index is number => index !== null)
    .sort((a, b) => a - b)

  const correctIndexes = parseAnswerToArray(correctAnswer)
    .map(token => resolveOptionIndex(token, options))
    .filter((index): index is number => index !== null)
    .sort((a, b) => a - b)

  if (userIndexes.length === 0 || correctIndexes.length === 0) {
    return false
  }
  if (userIndexes.length !== correctIndexes.length) {
    return false
  }

  return userIndexes.every((index, i) => index === correctIndexes[i])
}
