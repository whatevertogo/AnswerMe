export interface SelectableChoiceOption {
  index: number
  label: string
  text: string
}

function toOptionLabel(index: number): string {
  return String.fromCharCode(65 + index)
}

function toOptionIndex(label: string): number {
  if (!label || label.length !== 1) return -1
  const code = label.toUpperCase().charCodeAt(0) - 65
  return code >= 0 ? code : -1
}

export function buildSelectableChoiceOptions(
  options: string[] | undefined
): SelectableChoiceOption[] {
  return (options || [])
    .map((text, index) => ({
      index,
      label: toOptionLabel(index),
      text: String(text ?? '')
    }))
    .filter(option => option.text.trim().length > 0)
}

export function normalizeChoiceAnswer(
  answer: string | string[] | undefined,
  availableLabels: string[],
  isMultipleChoice: boolean
): string | string[] {
  const availableSet = new Set(availableLabels)
  const currentAnswers = Array.isArray(answer)
    ? answer
    : String(answer || '')
        .split(',')
        .map(item => item.trim())
        .filter(Boolean)

  const normalized = currentAnswers.filter(item => availableSet.has(item))
  return isMultipleChoice ? normalized : (normalized[0] ?? '')
}

export function remapChoiceAnswerAfterOptionRemoved(
  answer: string | string[] | undefined,
  removedIndex: number,
  isMultipleChoice: boolean
): string | string[] {
  const currentAnswers = Array.isArray(answer)
    ? answer
    : String(answer || '')
        .split(',')
        .map(item => item.trim())
        .filter(Boolean)

  const remapped = currentAnswers
    .map(label => {
      const index = toOptionIndex(label)
      if (index < 0) return null
      if (index === removedIndex) return null
      if (index > removedIndex) return toOptionLabel(index - 1)
      return toOptionLabel(index)
    })
    .filter((label): label is string => Boolean(label))

  return isMultipleChoice ? remapped : (remapped[0] ?? '')
}
