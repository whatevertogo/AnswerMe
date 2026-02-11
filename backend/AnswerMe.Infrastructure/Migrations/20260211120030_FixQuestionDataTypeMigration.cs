using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnswerMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixQuestionDataTypeMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 修复 QuestionDataJson 中缺少 type 字段的问题
            // 对于已经有 QuestionDataJson 但缺少 type 字段的记录，根据 QuestionType 补充

            // 1. 单选题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    COALESCE(QuestionDataJson, '{}'),
                    '$.type',
                    'choice'
                )
                WHERE QuestionType = 'SingleChoice'
                  AND json_extract(QuestionDataJson, '$.type') IS NULL;
            ");

            // 2. 多选题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    COALESCE(QuestionDataJson, '{}'),
                    '$.type',
                    'choice'
                )
                WHERE QuestionType = 'MultipleChoice'
                  AND json_extract(QuestionDataJson, '$.type') IS NULL;
            ");

            // 3. 判断题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    COALESCE(QuestionDataJson, '{}'),
                    '$.type',
                    'boolean'
                )
                WHERE QuestionType = 'TrueFalse'
                  AND json_extract(QuestionDataJson, '$.type') IS NULL;
            ");

            // 4. 填空题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    COALESCE(QuestionDataJson, '{}'),
                    '$.type',
                    'fillBlank'
                )
                WHERE QuestionType = 'FillBlank'
                  AND json_extract(QuestionDataJson, '$.type') IS NULL;
            ");

            // 5. 简答题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    COALESCE(QuestionDataJson, '{}'),
                    '$.type',
                    'shortAnswer'
                )
                WHERE QuestionType = 'ShortAnswer'
                  AND json_extract(QuestionDataJson, '$.type') IS NULL;
            ");

            // 6. 对于完全缺失 QuestionDataJson 的记录，尝试从旧字段构建
            // 单选/多选题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    'type', 'choice',
                    'options', COALESCE(
                        (SELECT value FROM json_each(Options) WHERE json_valid(Options)),
                        json('[]')
                    ),
                    'correctAnswers', json_array(CorrectAnswer),
                    'explanation', COALESCE(Explanation, ''),
                    'difficulty', Difficulty
                )
                WHERE QuestionType IN ('SingleChoice', 'MultipleChoice')
                  AND (QuestionDataJson IS NULL OR QuestionDataJson = '');
            ");

            // 判断题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    'type', 'boolean',
                    'correctAnswer', CASE
                        WHEN lower(CorrectAnswer) IN ('true', 't', '1', '对', '正确', 'yes', 'y') THEN 'true'
                        ELSE 'false'
                    END,
                    'explanation', COALESCE(Explanation, ''),
                    'difficulty', Difficulty
                )
                WHERE QuestionType = 'TrueFalse'
                  AND (QuestionDataJson IS NULL OR QuestionDataJson = '');
            ");

            // 填空题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    'type', 'fillBlank',
                    'acceptableAnswers', json_array(CorrectAnswer),
                    'explanation', COALESCE(Explanation, ''),
                    'difficulty', Difficulty
                )
                WHERE QuestionType = 'FillBlank'
                  AND (QuestionDataJson IS NULL OR QuestionDataJson = '');
            ");

            // 简答题
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    'type', 'shortAnswer',
                    'referenceAnswer', COALESCE(CorrectAnswer, ''),
                    'explanation', COALESCE(Explanation, ''),
                    'difficulty', Difficulty
                )
                WHERE QuestionType = 'ShortAnswer'
                  AND (QuestionDataJson IS NULL OR QuestionDataJson = '');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：清除 type 字段（实际应用中通常不会回滚数据迁移）
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_remove(QuestionDataJson, '$.type')
                WHERE json_extract(QuestionDataJson, '$.type') IS NOT NULL;
            ");
        }
    }
}
