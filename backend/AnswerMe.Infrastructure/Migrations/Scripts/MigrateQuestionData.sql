-- 数据迁移脚本：将旧格式转换为新格式
-- 用途：将现有的 Options 和 CorrectAnswer 转换到 QuestionDataJson

-- 步骤 1: 创建备份表
CREATE TABLE IF NOT EXISTS Questions_backup AS
SELECT * FROM Questions;

-- 步骤 2: 迁移选择题数据
-- 将 choice/single/multiple 类型的题目转换为 ChoiceQuestionData
UPDATE Questions
SET QuestionDataJson = json_object(
    'Options', json_extract(Options, '$'),
    'CorrectAnswers', json_array(CorrectAnswer),
    'Explanation', coalesce(Explanation, ''),
    'Difficulty', Difficulty,
    '$type', 'ChoiceQuestionData'
)
WHERE QuestionType IN ('choice', 'single', 'multiple', 'multiple-choice')
  AND QuestionDataJson IS NULL;

-- 步骤 3: 迁移判断题数据
-- 如果题目没有选项但正确答案是 true/false，转换为 BooleanQuestionData
UPDATE Questions
SET QuestionDataJson = json_object(
    'CorrectAnswer', CASE
        WHEN lower(CorrectAnswer) IN ('true', '1', 'yes', '对', '正确') THEN 'true'
        WHEN lower(CorrectAnswer) IN ('false', '0', 'no', '错', '错误') THEN 'false'
        ELSE 'false'
    END,
    'Explanation', coalesce(Explanation, ''),
    'Difficulty', Difficulty,
    '$type', 'BooleanQuestionData'
)
WHERE QuestionType IN ('true-false', 'boolean', 'bool')
  AND QuestionDataJson IS NULL;

-- 步骤 4: 迁移填空题数据
UPDATE Questions
SET QuestionDataJson = json_object(
    'AcceptableAnswers', json_array(CorrectAnswer),
    'Explanation', coalesce(Explanation, ''),
    'Difficulty', Difficulty,
    '$type', 'FillBlankQuestionData'
)
WHERE QuestionType IN ('fill', 'fill-blank', 'fillblank')
  AND QuestionDataJson IS NULL;

-- 步骤 5: 迁移简答题数据
UPDATE Questions
SET QuestionDataJson = json_object(
    'ReferenceAnswer', CorrectAnswer,
    'Explanation', coalesce(Explanation, ''),
    'Difficulty', Difficulty,
    '$type', 'ShortAnswerQuestionData'
)
WHERE QuestionType IN ('essay', 'short-answer', 'shortanswer')
  AND QuestionDataJson IS NULL;

-- 步骤 6: 更新题型为标准枚举值
UPDATE Questions
SET QuestionType = CASE
    WHEN QuestionType IN ('choice', 'single', 'single-choice') THEN 'SingleChoice'
    WHEN QuestionType IN ('multiple', 'multiple-choice') THEN 'MultipleChoice'
    WHEN QuestionType IN ('true-false', 'boolean', 'bool') THEN 'TrueFalse'
    WHEN QuestionType IN ('fill', 'fill-blank', 'fillblank') THEN 'FillBlank'
    WHEN QuestionType IN ('essay', 'short-answer', 'shortanswer') THEN 'ShortAnswer'
    ELSE QuestionType
END;

-- 步骤 7: 创建索引
CREATE INDEX IF NOT EXISTS idx_questions_type ON Questions(QuestionType);
CREATE INDEX IF NOT EXISTS idx_questions_questionbank ON Questions(QuestionBankId);

-- 步骤 8: 验证迁移结果
SELECT
    COUNT(*) as total_questions,
    COUNT(QuestionDataJson) as migrated_questions,
    COUNT(*) - COUNT(QuestionDataJson) as failed_migrations
FROM Questions;

-- 查看失败的迁移（如果有）
SELECT Id, QuestionText, QuestionType, QuestionDataJson
FROM Questions
WHERE QuestionDataJson IS NULL
LIMIT 10;
