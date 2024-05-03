using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region ExecuteType
    /// <summary>
    /// 執行類型
    /// </summary>
    public enum ExecuteType { stInstant, stBatch };
    /// <summary>
    /// 執行時間點
    /// </summary>
    public enum EventTime { etBefore, etAfter };
    #endregion

    #region ICommandExecutor<T>
    /// <summary>
    /// 策略執行器介面
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public interface ICommandExecutor : IOpMessage
    {
        /// <summary>
        /// 命令名稱
        /// </summary>
        string CommandName { get; }
        /// <summary>
        /// 執行器類型
        /// </summary>
        ExecuteType Type { get; }
        /// <summary>
        /// 執行時間點
        /// </summary>
        EventTime Timing { get; }
        /// <summary>
        /// 執行動作
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        bool Execute();
    }
    #endregion

    #region ICommandDataRegistry<T>
    /// <summary>
    /// 命令資料註冊
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommandDataRegistry<T> : ICommandExecutor, IDisposable
    {   
        /// <summary>
        /// 註冊處理資料
        /// </summary>
        /// <param name="data"></param>
        void RegistrationData(T data);
    }
    #endregion

    #region ICommandInvoker
    /// <summary>
    /// 命令召喚者
    /// </summary>
    public interface ICommandInvoker<T1,T2> : IOpMessage
    {
        /// <summary>
        /// 結果
        /// </summary>
        T1 CommandResult { get; }
        /// <summary>
        /// 執行命令
        /// </summary>
        /// <returns></returns>
        bool Run(T2 parameter);
    }
    #endregion

    #region IAsyncCommandExecutor<T>
    /// <summary>
    /// 非同步命令執行器
    /// </summary>
    public interface IAsyncCommandExecutor : IOpMessage, IDisposable
    {
        /// <summary>
        /// 命令名稱
        /// </summary>
        string CommandName { get; }
        /// <summary>
        /// 執行器類型
        /// </summary>
        ExecuteType Type { get; }
        /// <summary>
        /// 執行時間點
        /// </summary>
        EventTime Timing { get; }
        /// <summary>
        /// 執行動作
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task<bool> Execute();

        /// <summary>
        /// 註冊處理資料
        /// </summary>
        /// <param name="data"></param>
        void RegistrationData(object data);

        /// <summary>
        /// 註冊QC處理Logger
        /// </summary>
        /// <param name="data"></param>
        void RegistrationOperationContext(QCOperationContext operationContext);
    }
    #endregion
}
