import { describe, it, expect } from 'vitest'
import {
  QuestionType,
  getQuestionTypeLabel,
  isChoiceQuestionData,
  isBooleanQuestionData,
  isFillBlankQuestionData,
  isShortAnswerQuestionData,
  getQuestionOptions,
  getQuestionCorrectAnswers,
  isMultipleChoiceQuestion,
  isSingleChoiceQuestion,
  isBooleanQuestion,
  isFillBlankQuestion,
  isShortAnswerQuestion,
  toQuizType,
  fromQuizType
} from '../question'
import type { Question, QuestionData } from '../question'

describe('question type utilities', () => {
  describe('getQuestionTypeLabel', () => {
    it('应返回正确的题型显示名称', () => {
      expect(getQuestionTypeLabel(QuestionType.SingleChoice)).toBe('单选题')
      expect(getQuestionTypeLabel(QuestionType.MultipleChoice)).toBe('多选题')
      expect(getQuestionTypeLabel(QuestionType.TrueFalse)).toBe('判断题')
      expect(getQuestionTypeLabel(QuestionType.FillBlank)).toBe('填空题')
      expect(getQuestionTypeLabel(QuestionType.ShortAnswer)).toBe('简答题')
    })

    it('对未知类型返回类型本身', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const unknownType = 'UnknownType' as any
      expect(getQuestionTypeLabel(unknownType)).toBe('UnknownType')
    })
  })

  describe('类型守卫', () => {
    const choiceData: QuestionData = {
      type: 'choice',
      options: ['A', 'B'],
      correctAnswers: ['A']
    }

    const booleanData: QuestionData = {
      type: 'boolean',
      correctAnswer: true
    }

    const fillBlankData: QuestionData = {
      type: 'fillBlank',
      acceptableAnswers: ['answer1', 'answer2']
    }

    const shortAnswerData: QuestionData = {
      type: 'shortAnswer',
      referenceAnswer: '参考答案'
    }

    it('isChoiceQuestionData 应正确识别选择题数据', () => {
      expect(isChoiceQuestionData(choiceData)).toBe(true)
      expect(isChoiceQuestionData(booleanData)).toBe(false)
      expect(isChoiceQuestionData(fillBlankData)).toBe(false)
    })

    it('isBooleanQuestionData 应正确识别判断题数据', () => {
      expect(isBooleanQuestionData(booleanData)).toBe(true)
      expect(isBooleanQuestionData(choiceData)).toBe(false)
      expect(isBooleanQuestionData(fillBlankData)).toBe(false)
    })

    it('isFillBlankQuestionData 应正确识别填空题数据', () => {
      expect(isFillBlankQuestionData(fillBlankData)).toBe(true)
      expect(isFillBlankQuestionData(choiceData)).toBe(false)
      expect(isFillBlankQuestionData(booleanData)).toBe(false)
    })

    it('isShortAnswerQuestionData 应正确识别简答题数据', () => {
      expect(isShortAnswerQuestionData(shortAnswerData)).toBe(true)
      expect(isShortAnswerQuestionData(choiceData)).toBe(false)
      expect(isShortAnswerQuestionData(booleanData)).toBe(false)
    })

    it('对 undefined 应返回 false', () => {
      expect(isChoiceQuestionData(undefined)).toBe(false)
      expect(isBooleanQuestionData(undefined)).toBe(false)
      expect(isFillBlankQuestionData(undefined)).toBe(false)
      expect(isShortAnswerQuestionData(undefined)).toBe(false)
    })
  })

  describe('getQuestionOptions', () => {
    it('应从选择题数据中提取选项', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.SingleChoice,
        data: { type: 'choice', options: ['A', 'B', 'C'], correctAnswers: ['A'] },
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionOptions(question)).toEqual(['A', 'B', 'C'])
    })

    it('对非选择题返回空数组', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.TrueFalse,
        data: { type: 'boolean', correctAnswer: true },
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionOptions(question)).toEqual([])
    })

    it('对没有 data 的题目返回空数组', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.SingleChoice,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionOptions(question)).toEqual([])
    })
  })

  describe('getQuestionCorrectAnswers', () => {
    it('应从选择题数据中提取正确答案数组', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.SingleChoice,
        data: { type: 'choice', options: ['A', 'B'], correctAnswers: ['A'] },
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionCorrectAnswers(question)).toEqual(['A'])
    })

    it('应从判断题数据中提取布尔值答案', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.TrueFalse,
        data: { type: 'boolean', correctAnswer: true },
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionCorrectAnswers(question)).toBe(true)
    })

    it('应从填空题数据中提取可接受答案列表', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.FillBlank,
        data: { type: 'fillBlank', acceptableAnswers: ['answer1', 'answer2'] },
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionCorrectAnswers(question)).toEqual(['answer1', 'answer2'])
    })

    it('应从简答题数据中提取参考答案', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.ShortAnswer,
        data: { type: 'shortAnswer', referenceAnswer: '参考答案' },
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionCorrectAnswers(question)).toBe('参考答案')
    })

    it('对未知类型返回空字符串', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: 'UnknownType' as QuestionType,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        data: { type: 'unknown' as any } as QuestionData,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(getQuestionCorrectAnswers(question)).toBe('')
    })
  })

  describe('题型判断函数', () => {
    it('isMultipleChoiceQuestion 应正确识别多选题', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.MultipleChoice,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(isMultipleChoiceQuestion(question)).toBe(true)
      expect(isSingleChoiceQuestion(question)).toBe(false)
    })

    it('isSingleChoiceQuestion 应正确识别单选题', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.SingleChoice,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(isSingleChoiceQuestion(question)).toBe(true)
      expect(isMultipleChoiceQuestion(question)).toBe(false)
    })

    it('isBooleanQuestion 应正确识别判断题', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.TrueFalse,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(isBooleanQuestion(question)).toBe(true)
      expect(isFillBlankQuestion(question)).toBe(false)
    })

    it('isFillBlankQuestion 应正确识别填空题', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.FillBlank,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(isFillBlankQuestion(question)).toBe(true)
      expect(isShortAnswerQuestion(question)).toBe(false)
    })

    it('isShortAnswerQuestion 应正确识别简答题', () => {
      const question: Question = {
        id: 1,
        questionBankId: 1,
        questionText: 'Test',
        questionTypeEnum: QuestionType.ShortAnswer,
        difficulty: 'medium',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
      expect(isShortAnswerQuestion(question)).toBe(true)
      expect(isFillBlankQuestion(question)).toBe(false)
    })
  })

  describe('Quiz 类型转换', () => {
    it('toQuizType 应正确转换题型到 Quiz 简短值', () => {
      expect(toQuizType(QuestionType.SingleChoice)).toBe('single')
      expect(toQuizType(QuestionType.MultipleChoice)).toBe('multiple')
      expect(toQuizType(QuestionType.TrueFalse)).toBe('boolean')
      expect(toQuizType(QuestionType.FillBlank)).toBe('fill')
      expect(toQuizType(QuestionType.ShortAnswer)).toBe('essay')
    })

    it('fromQuizType 应正确转换 Quiz 简短值到题型', () => {
      expect(fromQuizType('single')).toBe(QuestionType.SingleChoice)
      expect(fromQuizType('multiple')).toBe(QuestionType.MultipleChoice)
      expect(fromQuizType('boolean')).toBe(QuestionType.TrueFalse)
      expect(fromQuizType('fill')).toBe(QuestionType.FillBlank)
      expect(fromQuizType('essay')).toBe(QuestionType.ShortAnswer)
    })

    it('toQuizType 对未知类型应返回默认值', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const unknownType = 'UnknownType' as any
      expect(toQuizType(unknownType)).toBe('essay')
    })

    it('fromQuizType 对未知类型应返回默认值', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const unknownQuizType = 'unknown' as 'single' | 'multiple' | 'boolean' | 'fill' | 'essay'
      expect(fromQuizType(unknownQuizType)).toBe(QuestionType.ShortAnswer)
    })
  })
})
