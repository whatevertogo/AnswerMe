using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnswerMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionDataJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加新列
            migrationBuilder.AddColumn<string>(
                name: "QuestionDataJson",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            // 创建备份表
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS questions_backup AS
                SELECT * FROM Questions;
            ");

            // 数据迁移：将旧格式转换为新格式
            // 1. 迁移选择题数据
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    '$type', 'ChoiceQuestionData',
                    'Options', json_extract(Options, '$'),
                    'CorrectAnswers', json_array(CorrectAnswer),
                    'Explanation', coalesce(Explanation, ''),
                    'Difficulty', Difficulty
                )
                WHERE QuestionType IN ('choice', 'single', 'multiple', 'single-choice', 'multiple-choice')
                  AND Options IS NOT NULL
                  AND QuestionDataJson IS NULL;
            ");

            // 2. 迁移判断题数据
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    '$type', 'BooleanQuestionData',
                    'CorrectAnswer', CASE
                        WHEN lower(CorrectAnswer) IN ('true', 't', '对', '正确', 'yes', 'y') THEN 'true'
                        ELSE 'false'
                    END,
                    'Explanation', coalesce(Explanation, ''),
                    'Difficulty', Difficulty
                )
                WHERE QuestionType IN ('true-false', 'boolean', 'bool', 'true_false')
                  AND QuestionDataJson IS NULL;
            ");

            // 3. 迁移填空题数据
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    '$type', 'FillBlankQuestionData',
                    'AcceptableAnswers', json_array(CorrectAnswer),
                    'Explanation', coalesce(Explanation, ''),
                    'Difficulty', Difficulty
                )
                WHERE QuestionType IN ('fill', 'fill-blank', 'fill_blank')
                  AND QuestionDataJson IS NULL;
            ");

            // 4. 迁移简答题数据
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_object(
                    '$type', 'ShortAnswerQuestionData',
                    'ReferenceAnswer', CorrectAnswer,
                    'Explanation', coalesce(Explanation, ''),
                    'Difficulty', Difficulty
                )
                WHERE QuestionType IN ('essay', 'short-answer', 'short_answer')
                  AND QuestionDataJson IS NULL;
            ");

            // 5. 更新题型为枚举值
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionType = CASE
                    WHEN QuestionType IN ('choice', 'single', 'single-choice') THEN 'SingleChoice'
                    WHEN QuestionType IN ('multiple', 'multiple-choice') THEN 'MultipleChoice'
                    WHEN QuestionType IN ('true-false', 'boolean', 'bool') THEN 'TrueFalse'
                    WHEN QuestionType IN ('fill', 'fill-blank', 'fill_blank') THEN 'FillBlank'
                    WHEN QuestionType IN ('essay', 'short-answer', 'short_answer') THEN 'ShortAnswer'
                    ELSE QuestionType
                END;
            ");

            // 6. 创建索引
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_questions_type ON Questions(QuestionType);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：删除索引
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_questions_type;");

            // 回滚：恢复旧题型格式
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionType = CASE
                    WHEN QuestionType = 'SingleChoice' THEN 'choice'
                    WHEN QuestionType = 'MultipleChoice' THEN 'multiple'
                    WHEN QuestionType = 'TrueFalse' THEN 'true-false'
                    WHEN QuestionType = 'FillBlank' THEN 'fill-blank'
                    WHEN QuestionType = 'ShortAnswer' THEN 'essay'
                    ELSE QuestionType
                END;
            ");

            migrationBuilder.DropColumn(
                name: "QuestionDataJson",
                table: "Questions");
        }
    }
}
