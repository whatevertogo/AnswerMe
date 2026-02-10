import { computed } from 'vue'
import type { GeneratedQuestion } from '@/api/aiGeneration'
import {
  isChoiceQuestionData,
  isBooleanQuestionData,
  isFillBlankQuestionData,
  isShortAnswerQuestionData
} from '@/types/question'

/**
 * 题目显示相关的composable
 * 提供题目类型、难度等显示逻辑
 */
export function useQuestionDisplay() {
  /**
   * 获取难度显示信息
   */
  const getDifficulty = computed(() => {
    return (difficulty: string) => {
      const map: Record<string, { text: string; type: 'success' | 'warning' | 'danger' | 'info' }> = {
        easy: { text: '简单', type: 'success' },
        medium: { text: '中等', type: 'warning' },
        hard: { text: '困难', type: 'danger' }
      }
      return map[difficulty] || { text: difficulty, type: 'info' }
    }
  })

  /**
   * 获取题型文本
   */
  const getQuestionTypeText = (type: string): string => {
    const map: Record<string, string> = {
      single: '单选题',
      multiple: '多选题',
      boolean: '判断题',
      fill: '填空题',
      essay: '简答题'
    }
    return map[type] || type
  }

  /**
   * 格式化题目文本用于复制
   * 使用新格式 data 属性
   */
  const formatQuestionForCopy = (question: GeneratedQuestion): string => {
    // 选项文本
    let optionsText = ''
    if (isChoiceQuestionData(question.data)) {
      optionsText = question.data.options
        .map((opt, idx) => `${String.fromCharCode(65 + idx)}. ${opt}`)
        .join('\n') + '\n'
    }

    // 答案文本
    let answerText = ''
    if (isChoiceQuestionData(question.data)) {
      answerText = question.data.correctAnswers.join(', ')
    } else if (isBooleanQuestionData(question.data)) {
      answerText = question.data.correctAnswer ? '正确' : '错误'
    } else if (isFillBlankQuestionData(question.data)) {
      answerText = question.data.acceptableAnswers.join(', ')
    } else if (isShortAnswerQuestionData(question.data)) {
      answerText = question.data.referenceAnswer
    } else {
      // Fallback for generated questions that might use old format
      answerText = String(question.correctAnswer || '')
    }

    return `【${question.questionType}】${question.questionText}\n${optionsText}答案: ${answerText}${
      question.explanation ? `\n解析: ${question.explanation}` : ''
    }`
  }

  return {
    getDifficulty,
    getQuestionTypeText,
    formatQuestionForCopy
  }
}
