import { computed } from 'vue'
import type { GeneratedQuestion } from '@/api/aiGeneration'
import {
  isChoiceQuestionData,
  isBooleanQuestionData,
  isFillBlankQuestionData,
  isShortAnswerQuestionData
} from '@/types/question'

/**
 * 格式化题目文本用于复制
 * 导出为独立函数以便复用
 */
export function formatQuestionForCopy(question: GeneratedQuestion): string {
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

/**
 * 题目显示相关的composable
 * 提供题目类型、难度等显示逻辑
 */
export function useQuestionDisplay() {
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

  return {
    getQuestionTypeText,
    formatQuestionForCopy
  }
}
