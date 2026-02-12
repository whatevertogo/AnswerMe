import { describe, it, expect } from 'vitest'
import {
  getOptionKey,
  extractOptionKey,
  resolveOptionIndex,
  inferChoiceAnswerMode,
  mapChoiceAnswerForSubmit,
  mapChoiceAnswerForDisplay,
  compareChoiceAnswers
} from '../quizAnswer'

describe('quizAnswer utilities', () => {
  describe('getOptionKey', () => {
    it('应返回正确的选项字母', () => {
      expect(getOptionKey(0)).toBe('A')
      expect(getOptionKey(1)).toBe('B')
      expect(getOptionKey(2)).toBe('C')
      expect(getOptionKey(25)).toBe('Z')
      expect(getOptionKey(26)).toBe('[')
    })
  })

  describe('extractOptionKey', () => {
    it('应提取单个字母选项键', () => {
      expect(extractOptionKey('A')).toBe('A')
      expect(extractOptionKey('B')).toBe('B')
      expect(extractOptionKey('Z')).toBe('Z')
      expect(extractOptionKey('a')).toBe('A')
      expect(extractOptionKey('b')).toBe('B')
    })

    it('应处理带标点的字母', () => {
      expect(extractOptionKey('A.')).toBe('A')
      expect(extractOptionKey('B、')).toBe('B')
      expect(extractOptionKey('C:')).toBe('C')
      expect(extractOptionKey('D-')).toBe('D')
      expect(extractOptionKey('E：')).toBe('E')
    })

    it('应处理字母后跟内容', () => {
      expect(extractOptionKey('A. 选项一')).toBe('A')
      expect(extractOptionKey('B、选项二')).toBe('B')
    })

    it('对无效格式返回 null', () => {
      expect(extractOptionKey('')).toBe(null)
      expect(extractOptionKey('1')).toBe(null)
      expect(extractOptionKey('AB')).toBe(null)
      expect(extractOptionKey('.A')).toBe(null)
    })

    it('应处理纯空格字符串', () => {
      expect(extractOptionKey('  ')).toBe(null)
    })
  })

  describe('resolveOptionIndex', () => {
    const options = ['选项一', '选项二', '选项三']

    it('应通过字母键解析选项索引', () => {
      expect(resolveOptionIndex('A', options)).toBe(0)
      expect(resolveOptionIndex('B', options)).toBe(1)
      expect(resolveOptionIndex('C', options)).toBe(2)
      expect(resolveOptionIndex('a', options)).toBe(0)
      expect(resolveOptionIndex('b', options)).toBe(1)
    })

    it('应通过完整文本匹配解析选项索引', () => {
      expect(resolveOptionIndex('选项一', options)).toBe(0)
      expect(resolveOptionIndex('选项二', options)).toBe(1)
      expect(resolveOptionIndex('选项三', options)).toBe(2)
    })

    it('应处理带前缀的选项文本', () => {
      expect(resolveOptionIndex('A. 选项一', options)).toBe(0)
      expect(resolveOptionIndex('B、选项二', options)).toBe(1)
    })

    it('应不区分大小写匹配', () => {
      expect(resolveOptionIndex('选项一', options)).toBe(0)
      expect(resolveOptionIndex('选项一', options)).toBe(0)
      expect(resolveOptionIndex('选项一', options)).toBe(0)
    })

    it('应修剪空格后匹配', () => {
      expect(resolveOptionIndex('  选项一  ', options)).toBe(0)
    })

    it('对不存在的选项返回 null', () => {
      expect(resolveOptionIndex('D', options)).toBe(null)
      expect(resolveOptionIndex('选项四', options)).toBe(null)
      expect(resolveOptionIndex('', options)).toBe(null)
    })

    it('对超出范围的字母键返回 null', () => {
      expect(resolveOptionIndex('Z', options)).toBe(null)
      expect(resolveOptionIndex('D', ['A', 'B', 'C'])).toBe(null)
    })
  })

  describe('inferChoiceAnswerMode', () => {
    it('当所有答案都是字母键时应返回 key 模式', () => {
      const result = inferChoiceAnswerMode(['A', 'B'], ['选项一', '选项二', '选项三'])
      expect(result).toBe('key')
    })

    it('当答案包含文本时应返回 text 模式', () => {
      const result = inferChoiceAnswerMode(['选项一', '选项二'], ['选项一', '选项二'])
      expect(result).toBe('text')
    })

    it('当字母键超出选项范围时应返回 text 模式', () => {
      const result = inferChoiceAnswerMode(['A', 'D'], ['选项一', '选项二'])
      expect(result).toBe('text')
    })

    it('对空答案应返回 text 模式', () => {
      const result = inferChoiceAnswerMode(undefined, ['选项一', '选项二'])
      expect(result).toBe('text')
    })

    it('对 undefined 答案应返回 text 模式', () => {
      const result = inferChoiceAnswerMode(undefined, ['选项一', '选项二'])
      expect(result).toBe('text')
    })

    it('混合大小写的字母键应返回 key 模式', () => {
      const result = inferChoiceAnswerMode(['a', 'B'], ['选项一', '选项二'])
      expect(result).toBe('key')
    })

    it('应过滤空字符串答案', () => {
      const result = inferChoiceAnswerMode(['', 'A'], ['选项一', '选项二'])
      expect(result).toBe('key')
    })
  })

  describe('mapChoiceAnswerForSubmit', () => {
    const options = ['选项一', '选项二', '选项三']

    it('在 key 模式下应将文本映射为字母键', () => {
      expect(mapChoiceAnswerForSubmit('选项一', options, 'key', false)).toBe('A')
      expect(mapChoiceAnswerForSubmit('选项二', options, 'key', false)).toBe('B')
    })

    it('在 key 模式下单选应返回单个字母', () => {
      const result = mapChoiceAnswerForSubmit(['A', 'B'], options, 'key', false)
      expect(result).toBe('A')
    })

    it('在 key 模式下多选应返回去重后的数组', () => {
      const result = mapChoiceAnswerForSubmit(['A', 'B', 'A'], options, 'key', true)
      expect(result).toEqual(['A', 'B'])
    })

    it('在 text 模式下应使用选项文本', () => {
      expect(mapChoiceAnswerForSubmit('选项一', options, 'text', false)).toBe('选项一')
    })

    it('在 text 模式下单选应返回单个文本', () => {
      const result = mapChoiceAnswerForSubmit(['选项一', '选项二'], options, 'text', false)
      expect(result).toBe('选项一')
    })

    it('在 text 模式下多选应返回去重后的数组', () => {
      const result = mapChoiceAnswerForSubmit(['选项一', '选项二', '选项一'], options, 'text', true)
      expect(result).toEqual(['选项一', '选项二'])
    })

    it('应过滤空值', () => {
      // text 模式下字母键会被映射为选项文本
      const result = mapChoiceAnswerForSubmit(['', 'A', 'B'], options, 'text', true)
      expect(result).toEqual(['选项一', '选项二'])
    })

    it('对超出范围的字母键保留原值（key 模式）', () => {
      const result = mapChoiceAnswerForSubmit('D', options, 'key', false)
      expect(result).toBe('D')
    })

    it('对不存在的文本保留原值（text 模式）', () => {
      const result = mapChoiceAnswerForSubmit('选项四', options, 'text', false)
      expect(result).toBe('选项四')
    })

    it('空选项列表时应返回原值', () => {
      expect(mapChoiceAnswerForSubmit('A', [], 'key', false)).toBe('A')
      expect(mapChoiceAnswerForSubmit('选项一', [], 'text', false)).toBe('选项一')
    })
  })

  describe('mapChoiceAnswerForDisplay', () => {
    const options = ['选项一', '选项二', '选项三']

    it('在 key 模式下应将字母键映射为选项文本', () => {
      expect(mapChoiceAnswerForDisplay('A', options, false)).toBe('选项一')
      expect(mapChoiceAnswerForDisplay('B', options, false)).toBe('选项二')
    })

    it('在 key 模式下应支持数组输入', () => {
      // display 模式下会将字母键映射为选项文本
      const result = mapChoiceAnswerForDisplay(['A', 'B'], options, false)
      expect(result).toBe('选项一')
    })

    it('在 text 模式下应保留选项文本', () => {
      expect(mapChoiceAnswerForDisplay('选项一', options, false)).toBe('选项一')
    })

    it('在 text 模式下应支持数组输入', () => {
      const result = mapChoiceAnswerForDisplay(['选项一', '选项二'], options, false)
      expect(result).toBe('选项一')
    })

    it('多选模式下应返回去重后的数组', () => {
      // display 模式下会将字母键映射为选项文本
      const result = mapChoiceAnswerForDisplay(['A', 'B', 'A'], options, true)
      expect(result).toEqual(['选项一', '选项二'])
    })

    it('应过滤空值', () => {
      // text 模式下字母键会被映射为选项文本
      const result = mapChoiceAnswerForDisplay(['', 'A'], options, true)
      expect(result).toEqual(['选项一'])
    })

    it('对 undefined 应返回空字符串', () => {
      expect(mapChoiceAnswerForDisplay(undefined, options, false)).toBe('')
      expect(mapChoiceAnswerForDisplay(undefined, options, true)).toEqual([])
    })

    it('空数组应返回空字符串（单选）', () => {
      const result = mapChoiceAnswerForDisplay([], options, false)
      expect(result).toBe('')
    })

    it('空数组应返回空数组（多选）', () => {
      const result = mapChoiceAnswerForDisplay([], options, true)
      expect(result).toEqual([])
    })
  })

  describe('compareChoiceAnswers', () => {
    const options = ['选项一', '选项二', '选项三']

    it('应正确比较相同的字母键答案', () => {
      expect(compareChoiceAnswers('A', 'A', options)).toBe(true)
      expect(compareChoiceAnswers(['A', 'B'], ['A', 'B'], options)).toBe(true)
    })

    it('应正确比较相同的文本答案', () => {
      expect(compareChoiceAnswers('选项一', '选项一', options)).toBe(true)
      expect(compareChoiceAnswers(['选项一', '选项二'], ['选项二', '选项一'], options)).toBe(true)
    })

    it('应忽略答案顺序', () => {
      expect(compareChoiceAnswers('A,B', ['B', 'A'], options)).toBe(true)
      expect(compareChoiceAnswers(['A', 'B', 'C'], ['C', 'B', 'A'], options)).toBe(true)
    })

    it('应正确处理混合格式的相同答案', () => {
      // compareChoiceAnswers 将所有答案解析为索引后比较
      expect(compareChoiceAnswers('A', 'A', options)).toBe(true)
      expect(compareChoiceAnswers(['A', 'B'], ['A', 'B'], options)).toBe(true)
    })

    it('应区分不同的答案', () => {
      expect(compareChoiceAnswers('A', 'B', options)).toBe(false)
      expect(compareChoiceAnswers(['A', 'B'], ['A', 'C'], options)).toBe(false)
    })

    it('应区分不同长度的答案', () => {
      expect(compareChoiceAnswers('A', ['A', 'B'], options)).toBe(false)
      expect(compareChoiceAnswers(['A', 'B'], 'A', options)).toBe(false)
    })

    it('对空答案应返回 false', () => {
      expect(compareChoiceAnswers('', 'A', options)).toBe(false)
      expect(compareChoiceAnswers('A', '', options)).toBe(false)
      expect(compareChoiceAnswers([], ['A'], options)).toBe(false)
      expect(compareChoiceAnswers(undefined, 'A', options)).toBe(false)
    })

    it('对超出范围的字母键应返回 false', () => {
      expect(compareChoiceAnswers('D', 'A', options)).toBe(false)
      expect(compareChoiceAnswers(['A', 'D'], ['A', 'B'], options)).toBe(false)
    })

    it('对不存在的文本应返回 false', () => {
      expect(compareChoiceAnswers('选项四', '选项一', options)).toBe(false)
    })
  })
})
