import { describe, expect, it } from 'vitest'
import {
  buildSelectableChoiceOptions,
  normalizeChoiceAnswer,
  remapChoiceAnswerAfterOptionRemoved
} from '@/utils/questionForm'

describe('questionForm utils', () => {
  it('should keep original option indexes when building selectable options', () => {
    const result = buildSelectableChoiceOptions(['Option A', '', 'Option C'])

    expect(result).toEqual([
      { index: 0, label: 'A', text: 'Option A' },
      { index: 2, label: 'C', text: 'Option C' }
    ])
  })

  it('should normalize answers by available labels', () => {
    const single = normalizeChoiceAnswer('B', ['A', 'C'], false)
    expect(single).toBe('')

    const multiple = normalizeChoiceAnswer(['A', 'B', 'C'], ['A', 'C'], true)
    expect(multiple).toEqual(['A', 'C'])
  })

  it('should remap answers after removing an option', () => {
    const single = remapChoiceAnswerAfterOptionRemoved('C', 1, false)
    expect(single).toBe('B')

    const multiple = remapChoiceAnswerAfterOptionRemoved(['A', 'C', 'D'], 2, true)
    expect(multiple).toEqual(['A', 'C'])
  })
})
