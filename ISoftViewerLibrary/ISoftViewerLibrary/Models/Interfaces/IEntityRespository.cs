using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IEntityRespository<T>
    /// <summary>
    /// 實體處理庫介面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityRespository<T> : IUnitOfWorkRepository, IRepository<T, List<ICommonFieldProperty>>
        where T : IElementInterface
    {
        /// <summary>
        /// ADD BY JB 20210416 要開放給外部指定查詢條件
        /// </summary>
        IElementInterface TableElement { get; set; }
        /// <summary>
        /// 取得內部的元素實例
        /// </summary>
        /// <returns></returns>
        T CloneElementInstance();
        /// <summary>
        /// 由於要支援客製表格且也要可以做Where查詢,所以提供Reset Table Schema的功能
        /// </summary>
        /// <returns></returns>
        bool PrepareElementSchema();
    }
    #endregion
}
