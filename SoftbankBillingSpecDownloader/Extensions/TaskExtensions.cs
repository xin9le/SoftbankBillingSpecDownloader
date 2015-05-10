using System;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace SoftbankBillingSpecDownloader.Extensions
{
    /// <summary>
    /// Task型の拡張機能を提供します。
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// 指定されたすべてのタスクが完了されてから完了するタスクを生成します。
        /// </summary>
        /// <typeparam name="TResult">タスクの戻り値の型</typeparam>
        /// <param name="tasks">タスクのコレクション</param>
        /// <returns>タスク</returns>
        public static Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));
            return Task.WhenAll(tasks);
        }
    }
}