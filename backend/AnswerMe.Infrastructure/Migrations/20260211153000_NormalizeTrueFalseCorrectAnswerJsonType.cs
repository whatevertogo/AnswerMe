using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AnswerMe.Infrastructure.Data;

#nullable disable

namespace AnswerMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AnswerMeDbContext))]
    [Migration("20260211153000_NormalizeTrueFalseCorrectAnswerJsonType")]
    public partial class NormalizeTrueFalseCorrectAnswerJsonType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 统一修复判断题 JSON 结构：
            // 1) 强制 type=boolean
            // 2) 将 correctAnswer 统一为 JSON bool（true/false），避免出现 text/integer 造成反序列化失败
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    json_set(
                        COALESCE(QuestionDataJson, '{}'),
                        '$.type',
                        'boolean'
                    ),
                    '$.correctAnswer',
                    CASE
                        WHEN lower(trim(COALESCE(json_extract(QuestionDataJson, '$.correctAnswer'), CorrectAnswer, ''))) IN ('true', 't', '1', 'yes', 'y', 'on', '对', '正确', '是') THEN json('true')
                        WHEN lower(trim(COALESCE(json_extract(QuestionDataJson, '$.correctAnswer'), CorrectAnswer, ''))) IN ('false', 'f', '0', 'no', 'n', 'off', '错', '错误', '否') THEN json('false')
                        ELSE json('false')
                    END
                )
                WHERE QuestionType = 'TrueFalse';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：将 bool 转回字符串（仅用于迁移回滚场景）
            migrationBuilder.Sql(@"
                UPDATE Questions
                SET QuestionDataJson = json_set(
                    QuestionDataJson,
                    '$.correctAnswer',
                    CASE
                        WHEN json_extract(QuestionDataJson, '$.correctAnswer') = 1 THEN 'true'
                        ELSE 'false'
                    END
                )
                WHERE QuestionType = 'TrueFalse';
            ");
        }
    }
}
