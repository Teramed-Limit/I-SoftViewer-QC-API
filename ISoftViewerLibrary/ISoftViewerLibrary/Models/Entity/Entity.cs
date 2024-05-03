using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Entity
{
    /// <summary>
    /// Entity抽象物件
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public abstract class Entity<T1,T2> : IInternalEventHandler
        where T1 : Value<T1>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="applier"></param>
        protected Entity(Action<object> applier) => ApplierFunc = applier;

        #region Fields
        /// <summary>
        /// 事件函式
        /// </summary>
        private readonly Action<object> ApplierFunc;
        /// <summary>
        /// 代表編號
        /// </summary>
        public T1 Id { get; protected set; }
        #endregion

        #region Methods
        /// <summary>
        /// 當....發生
        /// </summary>
        /// <param name="event"></param>
        protected abstract void When(object @event);
        /// <summary>
        /// 事件套用結果
        /// </summary>
        /// <param name="event"></param>
        protected void Apply(object @event)
        {
            When(@event);
            ApplierFunc(@event);
        }
        /// <summary>
        /// 執行事件的對應動作
        /// </summary>
        /// <param name="event"></param>
        void IInternalEventHandler.Handle(object @event) => When(@event);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<T2> GetEnumerator();
        #endregion
    }    
}
