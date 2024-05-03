using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IUnitOfWork
    /// <summary>
    /// 單元工作介面
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// 註冊對應的資料庫存取模式
        /// </summary>
        /// <param name="repository"></param>
        void RegisterRepository(IUnitOfWorkRepository repository);
        /// <summary>
        /// 處理訊息
        /// </summary>
        string OptMessage { get; }
    }
    #endregion

    #region IDBUnitOfWork
    /// <summary>
    /// 資料庫單元工作介面
    /// </summary>
    public interface IDBUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// 資料來源連線測試
        /// </summary>
        /// <returns></returns>
        bool DataSourceConnection();
        /// <summary>
        /// 關始進行資料庫交易
        /// </summary>
        void BeginTransaction();
        /// <summary>
        /// 確定寫回資料庫
        /// </summary>
        void Commit();
        /// <summary>
        /// 回復資料更新資料
        /// </summary>
        void Rollback();
        /// <summary>
        /// 資料庫結束連線
        /// </summary>
        void CloseConnection();
        /// <summary>
        /// 資料庫是否已連線 ADD BY JB 20210311
        /// </summary>
        bool IsDBConnected { get; }
    }
    #endregion

    #region IDcmUnitOfWork    
    /// <summary>
    /// DICOM處理作業
    /// </summary>
    public interface IDcmUnitOfWork : IOpMessage
    {
        /// <summary>
        /// 註冊處理庫
        /// </summary>
        /// <param name="dcmRepository"></param>
        void RegisterRepository(IDcmCqusDatasets dcmCqusDatasets);
        /// <summary>
        /// 開始DICOM網路服務
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        /// <returns></returns>
        bool Begin(string host, int port, string callingAe, string calledAe, DcmServiceUserType type, Dictionary<string,object> parameter = null);
        /// <summary>
        /// 確定處理
        /// </summary>
        /// <returns></returns>
        Task<bool> Commit();
        /// <summary>
        /// 取消復原
        /// </summary>
        Task<bool> Rollback();
    }
    #endregion
}
