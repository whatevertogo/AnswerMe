using Xunit;

namespace AnswerMe.Tests;

/// <summary>
/// 测试集合定义 - 防止并行执行导致数据库冲突
/// </summary>
[CollectionDefinition("DatabaseTests", DisableParallelization = true)]
public class DatabaseTestCollection
{
    // 此类不需要实现，仅用于定义集合
}
