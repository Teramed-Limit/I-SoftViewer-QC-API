using ISoftViewerLibrary.Models.Events;
using ISoftViewerLibrary.Models.Exceptions;
using ISoftViewerLibrary.Models.Interfaces;

namespace ISoftViewerLibrary.Models.DatabaseTables
{
    #region DicomStudyQCStatusTable
    /// <summary>
    /// DICOM Study唯一碼表格
    /// </summary>
    public class DicomStudyQCStatusTable : MasterDetailTable
    {        
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="userid"></param>
        public DicomStudyQCStatusTable(string userid)
            : base(userid, "DicomStudy")
        {
            
        }

        #region Fields
        /// <summary>
        /// 檢查唯一碼欄位
        /// </summary>
        public ICommonFieldProperty StudyInstanceUID { get; private set; }
        #endregion

        #region Methods  
        /// <summary>
        /// 指定Study Instance UID更新資料
        /// </summary>
        /// <param name="studyUID"></param>
        /// <returns></returns>
        public DicomStudyQCStatusTable SetInstanceUIDAndMaintainType(string studyUID, CommandFieldEvent.StudyMaintainType type, int value)
        {
            Apply(new CommandFieldEvent.OnStudyStatusUpdate() 
            {
                StudyInstanceUID = studyUID,
                StudyMaintainType = type,
                Value = value.ToString()
            });
            return this;
        }
        /// <summary>
        /// 事件觸發
        /// </summary>
        /// <param name="event"></param>
        protected override void When(object @event)
        {            
            switch (@event)
            {
                case CommandFieldEvent.OnStudyStatusUpdate e:
                    StudyInstanceUID = new TableFieldProperty()
                                .SetDbField("StudyInstanceUID", FieldType.ftString, isKey: true, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.StudyInstanceUID, "", null);
                    DBPrimaryKeyFields.Add(StudyInstanceUID);

                    var studyMaintainType = new TableFieldProperty()
                                .SetDbField(e.StudyMaintainType.ToString(), FieldType.ftInt, isKey: false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.Value, "", null);
                    DBNormalFields.Add(studyMaintainType);
                    break;
                default:
                    base.When(@event);
                    break;
            }
        }
        /// <summary>
        /// 確認資料是否正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;

            valid &= StudyInstanceUID.Value != string.Empty;

            if (!valid)
                throw new InvalidEntityStateException(this, "Post-checks failed in DicomStudyQCStatusTable");
        }
        #endregion        
    }
    #endregion
}
