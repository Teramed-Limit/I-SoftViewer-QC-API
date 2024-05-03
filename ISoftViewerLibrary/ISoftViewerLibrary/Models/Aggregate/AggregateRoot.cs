using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Aggregate
{
    #region AggregateRoot<T>
    /// <summary>
    /// 聚合物件抽象物件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AggregateRoot<T> : IInternalEventHandler
        where T : Value<T>
    {
        /// <summary>
        /// 建構
        /// </summary>
        protected AggregateRoot() => Changes = new List<object>();

        #region Fields
        /// <summary>
        /// 識別碼
        /// </summary>
        public T Id { get; protected set; }        
        /// <summary>
        /// 更新容器
        /// </summary>
        private readonly List<object> Changes;
        #endregion

        #region Methods
        /// <summary>
        /// 事件觸發
        /// </summary>
        /// <param name="event"></param>
        protected abstract void When(object @event);
        /// <summary>
        /// 套用事件
        /// </summary>
        /// <param name="event"></param>
        protected void Apply(object @event)
        {
            When(@event);
            EnsureValidState();
            Changes.Add(@event);
        }
        /// <summary>
        /// 取得事件列舉值
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetChanges() => Changes.AsEnumerable();
        /// <summary>
        /// 清除改變事件
        /// </summary>
        public void ClearChanges() => Changes.Clear();
        /// <summary>
        /// 驗證內容
        /// </summary>
        protected abstract void EnsureValidState();
        /// <summary>
        /// 套用到實體物件
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="event"></param>
        protected void ApplyToEntity(IInternalEventHandler entity, object @event)
            => entity?.Handle(@event);

        void IInternalEventHandler.Handle(object @event) => When(@event);
        #endregion
    }
    #endregion
}
