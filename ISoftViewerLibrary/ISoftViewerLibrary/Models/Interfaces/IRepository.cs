using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IWriteRepository<T>
    /// <summary>
    /// 寫入儲存庫界面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWriteRepository<T>
    {
        /// <summary>
        /// 新增或更新實體資料
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool AddOrUpdate(T entity);
    }
    #endregion

    #region IReadRepository<T,T1>
    /// <summary>
    /// 讀取儲存庫界面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadRepository<T>
    {
        /// <summary>
        /// 讀入並取得資料
        /// </summary>
        /// <returns></returns>
        T ReadAndGet();        
    }
    #endregion

    #region IRepository<T,T1>
    /// <summary>
    /// Reposity Interface 負責底層資料處理的介面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepository<T, T1> : IWriteRepository<T>, IRemoveRepository<T>, IInsertRepository<T>
        where T : IElementInterface
    {
        /// <summary>
        /// 依照Name去取得實體
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetData();        
        /// <summary>
        /// 取得所有實體資料
        /// </summary>
        /// <returns></returns>
        IEnumerable<T1> GetAll();
        /// <summary>
        /// 依照條件取得資料
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IEnumerable<T1> GetWhere(Func<T, bool> predicate);
        /// <summary>
        /// 取得所有筆數
        /// </summary>
        /// <returns></returns>
        int CountAll();
        /// <summary>
        /// 依照Name去取得實體,非同步版本
        /// </summary>
        /// <returns></returns>
        Task<T> GetDataAsync();
        /// <summary>
        /// 依照條件取得資料,非同步版本
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T1>> GetAllAsync();
        /// <summary>
        /// 依照條件取得資料,非同步版本
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<IEnumerable<T1>> GetWhereAsync(Func<T, bool> predicate);
        /// <summary>
        /// 取得所有筆數,非同步版本
        /// </summary>
        /// <returns></returns>
        Task<int> CountAllAsync();
    }
    #endregion

    #region IRemoveRepository
    /// <summary>
    /// 支援移除的處理庫
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T1"></typeparam>
    public interface IRemoveRepository<T>
        where T : IElementInterface
    {
        /// <summary>
        /// 刪除實體資料
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool Remove(T entity);
        /// <summary>
        /// 刪除實體資料,非同步版本
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(T entity);
    }
    #endregion

    #region IInsertRepository
    /// <summary>
    /// 支援新增的處理庫
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T1"></typeparam>
    public interface IInsertRepository<T>
        where T : IElementInterface
    {
        /// <summary>
        /// 新增實體資料
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool Insert(T entity);
        /// <summary>
        /// 新增實體資料,非同步版本
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> InsertAsync(T entity);
    }
    #endregion
}
