import { describe, it, expect } from 'vitest'
import { formatAnswerForSubmit, parseAnswerToArray, compareAnswers } from '../answerFormatter'

describe('answerFormatter', () => {
  describe('formatAnswerForSubmit', () => {
    it('应将数组答案格式化为 JSON 字符串', () => {
      const answer = ['A', 'B', 'C']
      expect(formatAnswerForSubmit(answer)).toBe('["A","B","C"]')
    })

    it('应过滤掉数组中的空值', () => {
      const answer: string[] = ['A', '', 'B', 'C']
      expect(formatAnswerForSubmit(answer)).toBe('["A","B","C"]')
    })

    it('应将字符串答案原样返回', () => {
      const answer = 'A'
      expect(formatAnswerForSubmit(answer)).toBe('A')
    })

    it('应修剪字符串答案的空格', () => {
      const answer = '  A  '
      expect(formatAnswerForSubmit(answer)).toBe('A')
    })

    it('应处理空字符串', () => {
      const answer = '  '
      expect(formatAnswerForSubmit(answer)).toBe('')
    })

    it('应处理空数组', () => {
      const answer: string[] = []
      expect(formatAnswerForSubmit(answer)).toBe('[]')
    })
  })

  describe('parseAnswerToArray', () => {
    it('应将字符串答案解析为单元素数组', () => {
      const answer = 'A'
      expect(parseAnswerToArray(answer)).toEqual(['A'])
    })

    it('应将数组答案原样返回', () => {
      const answer = ['A', 'B', 'C']
      expect(parseAnswerToArray(answer)).toEqual(['A', 'B', 'C'])
    })

    it('应解析 JSON 数组字符串', () => {
      const answer = '["A","B","C"]'
      expect(parseAnswerToArray(answer)).toEqual(['A', 'B', 'C'])
    })

    it('应忽略 JSON 数组字符串中的空格', () => {
      const answer = '["A", "B", "C"]'
      const result = parseAnswerToArray(answer) as string[]
      expect(result).toEqual(['A', 'B', 'C'])
    })

    it('应按逗号分隔字符串答案', () => {
      const answer = 'A,B,C'
      expect(parseAnswerToArray(answer)).toEqual(['A', 'B', 'C'])
    })

    it('应修剪逗号分隔后的空格', () => {
      const answer = ' A , B , C '
      expect(parseAnswerToArray(answer)).toEqual(['A', 'B', 'C'])
    })

    it('应处理无效的 JSON 数组字符串，回退到逗号分隔', () => {
      const answer = '[A,B,C]'
      expect(parseAnswerToArray(answer)).toEqual(['[A', 'B', 'C]'])
    })

    it('对 undefined 返回空数组', () => {
      expect(parseAnswerToArray(undefined)).toEqual([])
    })

    it('对空字符串返回空数组', () => {
      expect(parseAnswerToArray('')).toEqual([])
    })

    it('对纯空格字符串返回包含空格的数组', () => {
      // parseAnswerToArray 会按逗号分割，所以纯空格字符串会返回 ['   ']
      expect(parseAnswerToArray('   ')).toEqual(['   '])
    })
  })

  describe('compareAnswers', () => {
    it('应正确比较相同的字符串答案', () => {
      expect(compareAnswers('A', 'A')).toBe(true)
      expect(compareAnswers('A', 'B')).toBe(false)
    })

    it('应正确比较相同的数组答案', () => {
      expect(compareAnswers(['A', 'B'], ['B', 'A'])).toBe(true)
      expect(compareAnswers(['A', 'B'], ['A', 'C'])).toBe(false)
    })

    it('应正确比较不同格式的相同答案', () => {
      expect(compareAnswers('A,B', ['A', 'B'])).toBe(true)
      expect(compareAnswers(['A', 'B'], 'B,A')).toBe(true)
    })

    it('应区分不同长度的答案', () => {
      expect(compareAnswers(['A', 'B'], ['A', 'B', 'C'])).toBe(false)
    })

    it('应处理 undefined 答案', () => {
      expect(compareAnswers(undefined, 'A')).toBe(false)
      expect(compareAnswers('A', undefined)).toBe(false)
    })

    it('对 undefined 答案返回空数组比较', () => {
      expect(compareAnswers(undefined, undefined)).toBe(true)
    })

    it('应忽略答案顺序（数组）', () => {
      expect(compareAnswers(['A', 'B', 'C'], ['C', 'B', 'A'])).toBe(true)
    })

    it('应忽略答案顺序（字符串）', () => {
      expect(compareAnswers('A,B,C', 'C,A,B')).toBe(true)
    })

    it('应修剪答案中的空格后再比较', () => {
      expect(compareAnswers(' A , B ', ['A', 'B'])).toBe(true)
    })

    it('空数组 vs 非空数组应返回 false', () => {
      expect(compareAnswers([], ['A'])).toBe(false)
      expect(compareAnswers(['A'], [])).toBe(false)
    })

    it('两个空数组应被认为相等', () => {
      expect(compareAnswers([], [])).toBe(true)
    })

    it('字符串 vs 数组的相同答案应相等', () => {
      expect(compareAnswers('A,B,C', ['A', 'B', 'C'])).toBe(true)
    })
  })
})
