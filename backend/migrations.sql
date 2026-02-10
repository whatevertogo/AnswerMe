CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

CREATE TABLE "DataSources" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DataSources" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "Config" TEXT NOT NULL,
    "IsDefault" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DataSources_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserAIConfig" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_UserAIConfig" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "Provider" TEXT NOT NULL,
    "EncryptedApiKey" TEXT NOT NULL,
    "ApiBase" TEXT NULL,
    "ModelName" TEXT NULL,
    "DisplayName" TEXT NOT NULL,
    "IsEnabled" INTEGER NOT NULL,
    "LastVerifiedAt" TEXT NULL,
    "IsVerified" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_UserAIConfig_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "QuestionBanks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_QuestionBanks" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "DataSourceId" INTEGER NULL,
    "Tags" TEXT NOT NULL DEFAULT '[]',
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_QuestionBanks_DataSources_DataSourceId" FOREIGN KEY ("DataSourceId") REFERENCES "DataSources" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_QuestionBanks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Attempts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Attempts" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "QuestionBankId" INTEGER NOT NULL,
    "StartedAt" TEXT NOT NULL,
    "CompletedAt" TEXT NULL,
    "Score" NUMERIC(5,2) NULL,
    "TotalQuestions" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Attempts_QuestionBanks_QuestionBankId" FOREIGN KEY ("QuestionBankId") REFERENCES "QuestionBanks" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Attempts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Questions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Questions" PRIMARY KEY AUTOINCREMENT,
    "QuestionBankId" INTEGER NOT NULL,
    "QuestionText" TEXT NOT NULL,
    "QuestionType" TEXT NOT NULL,
    "Options" TEXT NULL,
    "CorrectAnswer" TEXT NOT NULL,
    "Explanation" TEXT NULL,
    "Difficulty" TEXT NOT NULL DEFAULT 'medium',
    "OrderIndex" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Questions_QuestionBanks_QuestionBankId" FOREIGN KEY ("QuestionBankId") REFERENCES "QuestionBanks" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AttemptDetails" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AttemptDetails" PRIMARY KEY AUTOINCREMENT,
    "AttemptId" INTEGER NOT NULL,
    "QuestionId" INTEGER NOT NULL,
    "UserAnswer" TEXT NULL,
    "IsCorrect" INTEGER NULL,
    "TimeSpent" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_AttemptDetails_Attempts_AttemptId" FOREIGN KEY ("AttemptId") REFERENCES "Attempts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AttemptDetails_Questions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES "Questions" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AttemptDetails_AttemptId" ON "AttemptDetails" ("AttemptId");

CREATE INDEX "IX_AttemptDetails_QuestionId" ON "AttemptDetails" ("QuestionId");

CREATE INDEX "IX_Attempts_QuestionBankId" ON "Attempts" ("QuestionBankId");

CREATE INDEX "IX_Attempts_UserId" ON "Attempts" ("UserId");

CREATE INDEX "IX_DataSources_Type" ON "DataSources" ("Type");

CREATE INDEX "IX_DataSources_UserId" ON "DataSources" ("UserId");

CREATE INDEX "IX_QuestionBanks_DataSourceId" ON "QuestionBanks" ("DataSourceId");

CREATE INDEX "IX_QuestionBanks_UserId" ON "QuestionBanks" ("UserId");

CREATE INDEX "IX_Questions_QuestionBankId" ON "Questions" ("QuestionBankId");

CREATE INDEX "IX_Questions_QuestionType" ON "Questions" ("QuestionType");

CREATE INDEX "IX_UserAIConfig_UserId" ON "UserAIConfig" ("UserId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260208173334_InitialCreate', '10.0.2');

COMMIT;

BEGIN TRANSACTION;
DROP TABLE "UserAIConfig";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260208175406_AddUserAIConfig', '10.0.2');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "QuestionBanks" ADD "Version" BLOB NOT NULL DEFAULT X'';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260208182949_AddVersionToQuestionBank', '10.0.2');

COMMIT;

