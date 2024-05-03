using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerQCSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Applications
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class DbQueryApplicationService<T1, T2> : IApplicationQueryService<T1>, IApplicationQueryEnumerateService<T1>
        where T1 : JsonDatasetBase, new() //external
        where T2 : JsonDatasetBase, new() //database
    {
        /// <summary>
        /// 建構
        /// </summary>
        public DbQueryApplicationService(ICommonRepositoryService<T2> repositoryService)
        {
            TableService = (DbTableService<T2>)repositoryService;
        }

        #region Fields
        /// <summary>
        /// DicomServiceProvider資料表處理服務
        /// </summary>
        protected DbTableService<T2> TableService;        
        #endregion

        #region Methods
        /// <summary>
        /// 處理回覆單筆資料
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public virtual Task<T1> Handle(string userName, object command)
        {
            IEnumerable<T2> tmp;
            if (command == null)
                tmp = TableService.GetAll();
            else
                tmp = TableService.Get(command as string);

            T1 result = ConvertData(tmp.FirstOrDefault());
            return Task.FromResult(result);
        }
        /// <summary>
        /// 處理回覆多筆資料
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public virtual Task<List<T1>> HandleMultiple(string userName, object command)
        {
            List<T1> result = new();

            var dataset = TableService.GetAll();
            foreach (var data in dataset)
            {
                T1 newData = NewData(data);
                result.Add(newData);
            }
            return Task.FromResult(result);
        }
        /// <summary>
        /// 產生新的轉換資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual T1 NewData(T2 data)
        {
            T1 newObject = (T1)Activator.CreateInstance(data.GetType());
            foreach (var originalProp in data.GetType().GetProperties())
            {
                originalProp.SetValue(newObject, originalProp.GetValue(data));
            }
            return newObject;
        }
        /// <summary>
        /// 轉換資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual T1 ConvertData(T2 data)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 取得特定資料
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual Task<List<string>> HandleMultiple(string userName)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
