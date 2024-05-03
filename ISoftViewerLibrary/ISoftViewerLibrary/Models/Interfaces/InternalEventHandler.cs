using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IInternalEventHandler
    /// <summary>
    /// Event執行控制器介面
    /// </summary>
    public interface IInternalEventHandler
    {
        void Handle(object @event);
    }
    #endregion
}
