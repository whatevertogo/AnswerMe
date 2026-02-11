using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnswerMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDataTypeMismatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 修复判断题被错误标记为 choice 类型的问题
            // 对于 QuestionType = TrueFalse 但 QuestionDataJson.type = choice 的记录
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    'type', 'boolean',
                    'correctAnswer', CASE
                        WHEN lower(CorrectAnswer) IN ('true', 't', '1', '对', '正确', 'yes', 'y') THEN 'true'
                        ELSE 'false'
                    END,
                    'explanation', COALESCE(
                        json_extract(QuestionDataJson, '$.explanation'),
                        COALESCE(Explanation, '')
                    ),
                    'difficulty', COALESCE(
                        json_extract(QuestionDataJson, '$.difficulty'),
                        Difficulty
                    )
                )
                WHERE QuestionType = 'TrueFalse'
                  AND json_extract(QuestionDataJson, '$.type') = 'choice';
            ");

            // 修复 SingleChoice 与 MultipleChoice 的区分问题
            // 根据 correctAnswers 数组长度来确定是单选还是多选
            // 如果 correctAnswers 只有一个元素，且 QuestionType 是 MultipleChoice，则改为 SingleChoice
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionType = 'SingleChoice'
                WHERE QuestionType = 'MultipleChoice'
                  AND json_extract(QuestionDataJson, '$.type') = 'choice'
                  AND json_array_length(json_extract(QuestionDataJson, '$.correctAnswers')) = 1;
            ");

            // 如果 correctAnswers 有多个元素，且 QuestionType 是 SingleChoice，则改为 MultipleChoice
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionType = 'MultipleChoice'
                WHERE QuestionType = 'SingleChoice'
                  AND json_extract(QuestionDataJson, '$.type') = 'choice'
                  AND json_array_length(json_extract(QuestionDataJson, '$.correctAnswers')) > 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：由于数据修改不可逆，这里不做操作
            // 实际使用中应该从备份恢复
        }
    }
}
