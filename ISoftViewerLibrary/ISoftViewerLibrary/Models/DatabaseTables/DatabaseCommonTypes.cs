
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.Events;
using ISoftViewerLibrary.Models.Exceptions;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static ISoftViewerLibrary.Models.Entity.DicomEntities;

namespace ISoftViewerLibrary.Models.DatabaseTables
{
    #region TableFieldProperty
    /// <summary>
    /// Field的屬性物件
    /// </summary>
    public class TableFieldProperty : AggregateRoot<CommandFieldId>, ICommonFieldProperty
    {
        /// <summary>
        /// 建構
        /// </summary>
        public TableFieldProperty()
        {
            dicomOperatorHelper = new DicomOperatorHelper();
            FieldName = "";
            Type = FieldType.ftString;
            IsPrimaryKey = false;
            DicomGroup = 0;
            DicomElem = 0;
            IsNull = true;
            Value = "";
            Value2nd = "";
            BinaryValue = null;
            UpdateSqlByPass = false;
            SqlOperator = FieldOperator.foAnd;
            OrderOperator = OrderOperator.foASC;
            State = TableFieldState.None;
            AliasFieldName = "";
        }
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="fieldname"></param>
        /// <param name="Type"></param>
        /// <param name="primarykey"></param>
        /// <param name="group"></param>
        /// <param name="elem"></param>
        protected TableFieldProperty(string fieldname, FieldType type, bool primarykey, string tag, bool isNull = true,
            bool updateByPass = false, FieldOperator foperator = FieldOperator.foAnd, OrderOperator foOrderOperator = OrderOperator.foNone)
        {
            dicomOperatorHelper = new DicomOperatorHelper();
            FieldName = fieldname;
            Type = type;
            IsPrimaryKey = primarykey;
            dicomOperatorHelper.ConvertTagStringToUIntGE(tag, out ushort g, out ushort e);
            DicomGroup = g;
            DicomElem = e;
            IsNull = isNull;
            Value = "";
            Value2nd = "";
            BinaryValue = null;
            UpdateSqlByPass = updateByPass;
            SqlOperator = foperator;
            OrderOperator = foOrderOperator;
            State = TableFieldState.None;
            AliasFieldName = "";
            if (FieldName.ToLower().Contains(" as ") == true)
            {
                int pos = FieldName.ToLower().IndexOf("as", 0, FieldName.Length);
                AliasFieldName = FieldName.Substring(pos + 2).Trim();
            }
        }
        /// <summary>
        /// 用來做複製物件使用
        /// </summary>
        /// <param name="fieldname"></param>
        /// <param name="type"></param>
        /// <param name="primaryKey"></param>
        /// <param name="group"></param>
        /// <param name="elem"></param>
        /// <param name="isNull"></param>
        /// <param name="value"></param>
        /// <param name="value2nd"></param>
        /// <param name="updateByPass"></param>
        /// <param name="binaryValue"></param>
        protected TableFieldProperty(string fieldname, FieldType type, bool primaryKey, ushort group, ushort elem, bool isNull,
            string value, string value2nd, bool updateByPass, byte[] binaryValue, FieldOperator foperator = FieldOperator.foAnd, OrderOperator foOrderOperator = OrderOperator.foNone)
        {
            FieldName = fieldname;
            Type = type;
            IsPrimaryKey = primaryKey;
            DicomGroup = group;
            DicomElem = elem;
            IsNull = isNull;
            Value = value;
            Value2nd = value2nd;
            BinaryValue = binaryValue;
            UpdateSqlByPass = updateByPass;
            SqlOperator = foperator;
            OrderOperator = foOrderOperator;
            State = TableFieldState.None;
            AliasFieldName = "";
            if (FieldName.ToLower().Contains(" as ") == true)
            {
                int pos = FieldName.ToLower().IndexOf("as", 0, FieldName.Length);
                AliasFieldName = FieldName.Substring(pos + 2).Trim();
            }
        }
        #region Fields
        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string FieldName { get; private set; }
        /// <summary>
        /// 欄位型態
        /// </summary>
        public FieldType Type { get; private set; }
        /// <summary>
        /// 是否為主鍵
        /// </summary>
        public bool IsPrimaryKey { get; private set; }
        /// <summary>
        /// DICOM Tag - Group
        /// </summary>
        public ushort DicomGroup { get; private set; }
        /// <summary>
        /// DICOM Tag - Elem
        /// </summary>
        public ushort DicomElem { get; private set; }
        /// <summary>
        /// 是否可以為空值
        /// </summary>
        public bool IsNull { get; private set; }
        /// <summary>
        /// 資料內容
        /// </summary>
        public string Value { get; private set; }
        /// <summary>
        /// 執行Update語法是否要略過此欄位
        /// </summary>
        public bool UpdateSqlByPass { get; private set; }
        /// <summary>
        /// 第二組資料內容,通常用來做Between的End資料用
        /// </summary>
        public string Value2nd { get; private set; }
        /// <summary>
        /// 二進位資料內容
        /// </summary>
        public byte[] BinaryValue { get; private set; }
        /// <summary>
        /// 該欄位是否支援全文檢索查詢        
        /// </summary>
        public bool IsSupportFullTextSearch { get; private set; }
        /// <summary>
        /// DICOM相關輔助物件
        /// </summary>
        protected DicomOperatorHelper dicomOperatorHelper;
        /// <summary>
        /// 用來做資料表查詢的運算子
        /// </summary>
        public FieldOperator SqlOperator { get; private set; }
        /// <summary>
        /// 排序
        /// </summary>
        public OrderOperator OrderOperator { get; private set; }
        /// <summary>
        /// 欄位別名
        /// </summary>
        public string AliasFieldName { get; set; }
        /// <summary>
        /// 內容狀態
        /// </summary>
        internal TableFieldState State;
        #endregion

        #region Methdos
        /// <summary>
        /// 清除欄位資料
        /// </summary>
        public void ResetValue()
        {
            Value = "";
            Value2nd = "";
            BinaryValue = null;
        }

        /// <summary>
        /// 自動產生SQL語法
        /// </summary>
        /// <returns></returns>
        public string MakeSQL(string whereSQL)
        {
            string result;
            if (SqlOperator == FieldOperator.foIn)
                result = FieldName + $" In ( {Value} ) ";
            else if (SqlOperator == FieldOperator.foBetween)
                result = FieldName + " Between '" + Value + "' And '" + Value2nd + "' ";
            else if (SqlOperator == FieldOperator.foLike)
                result = FieldName + " Like '" + Value + "' ";
            else if (SqlOperator == FieldOperator.forNot)
                result = FieldName + " != '" + Value + "' ";
            else
                result = FieldName + " = '" + Value + "' ";

            if (whereSQL != "")
            {
                if (SqlOperator == FieldOperator.foOr)
                    result = whereSQL + " Or " + result;
                else
                    result = whereSQL + " And " + result;
            }
            return result;
        }

        /// <summary>
        /// 深層複製
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new TableFieldProperty(FieldName, Type, IsPrimaryKey, DicomGroup, DicomElem, IsNull, Value, Value2nd, UpdateSqlByPass, BinaryValue);
        }
        /// <summary>
        /// 指定資料庫欄位
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="isKey"></param>
        /// <param name="allowNull"></param>
        /// <param name="updateByPass"></param>
        /// <param name="fullTextSearch"></param>
        /// <param name="fieldOperator"></param>
        /// <param name="orderOperator"></param>
        /// <returns></returns>
        public ICommonFieldProperty SetDbField(string name, FieldType type, bool isKey, bool allowNull, bool updateByPass, bool fullTextSearch,
            FieldOperator fieldOperator, OrderOperator orderOperator)
        {
            Apply(new CommandFieldEvent.DbFieldCreated
            {
                DbFieldCreateId = Guid.NewGuid(),
                FieldName = name,
                Type = type,
                IsPrimaryKey = isKey,
                IsNull = allowNull,
                UpdateSqlByPass = updateByPass,
                IsSupportFullTextSearch = fullTextSearch,
                SqlOperator = fieldOperator,
                OrderOperator = orderOperator
            }); ;
            Value = "";
            Value2nd = "";
            BinaryValue = null;
            return this;
        }
        /// <summary>
        /// 指定DICOM欄位資料
        /// </summary>
        /// <param name="group"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public ICommonFieldProperty SetDicomTag(ushort group, ushort elem)
        {
            Apply(new CommandFieldEvent.DicomFieldCreated
            {
                DcmFieldCreateId = Guid.NewGuid(),
                DicomGroup = group,
                DicomElem = elem
            });
            Value = "";
            Value2nd = "";
            BinaryValue = null;
            return this;
        }
        /// <summary>
        /// 指定資料庫欄位
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public ICommonFieldProperty UpdateDbFieldValues(string value1, string value2, byte[] buffer)
        {
            Apply(new CommandFieldEvent.DbFieldChanged
            {
                Value = value1,
                Value2nd = value2,
                BinaryValue = buffer
            });
            return this;
        }
        /// <summary>
        /// 指定DICOM資料
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ICommonFieldProperty UpdateDicomValues(string value)
        {
            Apply(new CommandFieldEvent.DicomValueChanged
            {
                Value = value
            });
            return this;
        }
        /// <summary>
        /// 更新資料及排序方式
        /// </summary>
        /// <param name="value"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public ICommonFieldProperty UpdateDbValueAndOrderBy(string value, OrderOperator order)
        {
            Apply(new CommandFieldEvent.UpdateDbValueAndOrder
            {
                Value = value,
                OrderOperator = order
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
                case CommandFieldEvent.DbFieldCreated e:
                    if (Id == null)
                        Id = new CommandFieldId(e.DbFieldCreateId);
                    FieldName = e.FieldName;
                    Type = e.Type;
                    IsPrimaryKey = e.IsPrimaryKey;
                    IsNull = e.IsNull;
                    UpdateSqlByPass = e.UpdateSqlByPass;
                    IsSupportFullTextSearch = e.IsSupportFullTextSearch;
                    SqlOperator = e.SqlOperator;
                    OrderOperator = e.OrderOperator;
                    State = TableFieldState.DbFieldCreated;
                    if (e.FieldName.ToLower().Contains(" as "))
                    {
                        int pos = FieldName.ToLower().IndexOf("as", 0, FieldName.Length);
                        AliasFieldName = FieldName.Substring(pos + 2).Trim();
                    }
                    break;
                case CommandFieldEvent.DicomFieldCreated e:
                    if (Id != null)
                        Id = new CommandFieldId(e.DcmFieldCreateId);
                    DicomGroup = e.DicomGroup;
                    DicomElem = e.DicomElem;
                    State = TableFieldState.DicomFieldCreated;
                    break;
                case CommandFieldEvent.DbFieldChanged e:
                    Value = e.Value;
                    Value2nd = e.Value2nd;
                    BinaryValue = e.BinaryValue;
                    State = TableFieldState.DbFieldValueUpdated;
                    break;
                case CommandFieldEvent.UpdateDbValueAndOrder e:
                    Value = e.Value;
                    OrderOperator = e.OrderOperator;
                    State = TableFieldState.DbFieldValueUpdated;
                    break;
            }
        }
        /// <summary>
        /// 强化及確保資料欄位正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = Id != null;
            switch (State)
            {
                case TableFieldState.DbFieldCreated:
                    valid = valid && (IsPrimaryKey ? IsNull == false : true);
                    break;
                // case TableFieldState.DbFieldValueUpdated:
                //     if (IsNull == false && (Value == string.Empty && Value2nd == string.Empty && BinaryValue == null))
                //         valid = false;
                //     break;
                case TableFieldState.DicomFieldCreated:
                    valid = valid && (DicomGroup != 0 && DicomElem != 0);
                    break;
            }
            if (!valid)
                throw new InvalidEntityStateException(this, $"Post-checks failed in state {State}");
        }
        #endregion

        #region enum
        public enum TableFieldState
        {
            None,
            DbFieldCreated,
            DbFieldValueUpdated,
            DicomFieldCreated
        }
        #endregion
    }
    #endregion

    #region CommandFieldId
    public class CommandFieldId : Value<CommandFieldId>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="value"></param>
        public CommandFieldId(Guid id)
        {
            Id = id;
        }

        #region Fields            
        /// <summary>
        /// 程式唯一碼
        /// </summary>
        public Guid Id { get; }
        #endregion
    }
    #endregion

    #region ElementAbstract
    /// <summary>
    /// Datasource Element抽象物件
    /// </summary>
    public class ElementAbstract : AggregateRoot<DicomSourceReference>, IElementInterface, IDisposable
    {
        /// <summary>
        /// 建構 MOD BY JB 20200615 若無預代使用者ID,則預設為Teramed administrator
        /// </summary>
        public ElementAbstract(string userId = "")
        {
            DBNormalFields = new List<ICommonFieldProperty>();
            DBPrimaryKeyFields = new List<ICommonFieldProperty>();
            DBDatasets = new List<List<ICommonFieldProperty>>();
            //MOD BY JB 20211025 改為强制資料判斷            
            CreateUser = new TableFieldProperty()
                            .SetDbField("CreateUser", FieldType.ftString, false, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                            .UpdateDbFieldValues((userId == "") ? "Teramed administrator" : userId, "", null);

            CreateDateTime = new TableFieldProperty()
                            .SetDbField("CreateDateTime", FieldType.ftString, false, false, true, false, FieldOperator.foAnd, OrderOperator.foNone);
            // NULL
            ModifiedUser = new TableFieldProperty()
                            .SetDbField("ModifiedUser", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                            .UpdateDbFieldValues((userId == "") ? "Teramed administrator" : userId, "", null);
            ModifiedDateTime = new TableFieldProperty()
                            .SetDbField("ModifiedDateTime", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone);

            DBNormalFields.Add(CreateUser);
            DBNormalFields.Add(CreateDateTime);
            DBNormalFields.Add(ModifiedUser);
            DBNormalFields.Add(ModifiedDateTime);

            TableName = "";
            //預設不置換資料
            ReplaceDataFromSelectExecutor = false;
            ElementProcessingMessages = new List<string>();

            IsSupportFullTextSearch = false;
            HaveDataRow = false;
        }
        /// <summary>
        /// 建構
        /// </summary>
        public ElementAbstract()
        {
            DBNormalFields = new List<ICommonFieldProperty>();
            DBPrimaryKeyFields = new List<ICommonFieldProperty>();
            DBDatasets = new List<List<ICommonFieldProperty>>();

            TableName = "";
            //預設不置換資料
            ReplaceDataFromSelectExecutor = false;
            ElementProcessingMessages = new List<string>();

            IsSupportFullTextSearch = false;
            HaveDataRow = false;
        }

        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="element"></param>
        public ElementAbstract(IElementInterface element)
        {
            TableName = element.TableName;
            UserId = element.UserId;

            DBPrimaryKeyFields = element.DBPrimaryKeyFields.ConvertAll(x =>
            {
                ICommonFieldProperty newField = x.Clone() as TableFieldProperty;
                return newField;
            });

            DBNormalFields = element.DBNormalFields.ConvertAll(x =>
            {
                ICommonFieldProperty newField = x.Clone() as TableFieldProperty;
                return newField;
            });
            //先前查詢時,會把CreateUser and ModifiedUser若資料庫沒有欄位時,此二個欄位並不會有資料
            //不一定每一個Table都有欄位
            element.DBNormalFields.ForEach((property =>
            {
                //MOD BY JB 20211025 改為强制資料判斷
                if (property.FieldName == "CreateUser" || property.FieldName == "ModifiedUser")
                    property.UpdateDbFieldValues(UserId, "", null);
            }));

            DBDatasets = element.DBDatasets.ConvertAll(x =>
            {
                List<ICommonFieldProperty> newRecord = new List<ICommonFieldProperty>();

                x.ForEach(y =>
                {
                    ICommonFieldProperty newField = y.Clone() as TableFieldProperty;
                    newRecord.Add(newField);
                });
                return newRecord;
            });
        }

        #region Fields
        /// <summary>
        /// 資料表格名稱
        /// </summary>
        public string TableName { get; protected set; }
        /// <summary>
        /// 用來存放非主鍵的欄位
        /// </summary>
        public List<ICommonFieldProperty> DBNormalFields { get; protected set; }
        /// <summary>
        /// 用來存放主鍵的欄位
        /// </summary>
        public List<ICommonFieldProperty> DBPrimaryKeyFields { get; protected set; }
        /// <summary>
        /// 從資料庫查詢回來的資料集合
        /// </summary>
        public List<List<ICommonFieldProperty>> DBDatasets { get; protected set; }
        /// <summary>
        /// 建立此筆資料的使用者帳號
        /// </summary>
        public ICommonFieldProperty CreateUser { get; set; }
        /// <summary>
        /// 建立此筆資料的日期時間
        /// </summary>
        public ICommonFieldProperty CreateDateTime { get; set; }
        /// <summary>
        /// 修改此筆資料的使用者帳號
        /// </summary>
        public ICommonFieldProperty ModifiedUser { get; set; }
        /// <summary>
        /// 修改此筆資料的日期時間
        /// </summary>
        public ICommonFieldProperty ModifiedDateTime { get; set; }
        /// <summary>
        /// 當使用查詢器時,要不要從查詢詢器置換資料
        /// </summary>
        public bool ReplaceDataFromSelectExecutor { get; set; }
        /// <summary>
        /// 處理過程的訊息
        /// </summary>
        public ICollection<string> ElementProcessingMessages { get; } = new List<string>();
        /// <summary>            
        /// 是否支援全文檢索
        /// </summary>
        public bool IsSupportFullTextSearch { get; set; }
        /// <summary>
        /// 使用者帳號
        /// </summary>
        public string UserId { get; protected set; }
        /// <summary>
        /// 記錄是否有查詢到資料
        /// </summary>
        public bool HaveDataRow { get; set; }
        /// <summary>
        /// 目前存取事件
        /// </summary>
        protected ElementState State;
        #endregion

        #region Methods
        /// <summary>
        /// 執行所有執行器
        /// </summary>
        /// <param name="executors"></param>
        public virtual bool Accept(List<IExecutorInterface> executors)
        {
            ElementProcessingMessages.Add("    { " + TableName + " } Start execution !! ");
            bool resultOfExecute = true;
            foreach (var executor in executors)
            {
                resultOfExecute = executor.Execute(this, resultOfExecute as object);
                //需記錄是否有任何執行訊息
                if (executor.Messages().Trim() != "")
                    ElementProcessingMessages.Add(executor.Messages());
            }
            ElementProcessingMessages.Add("    { " + TableName + " } End execution !! ");
            return resultOfExecute;
        }
        /// <summary>
        /// 清除已記錄的資料庫欄位內容及資料
        /// </summary>
        public virtual void ClearDBDatasets()
        {
            foreach (var item in DBDatasets)
            {
                item.Clear();
            }
        }
        /// <summary>
        /// 取得Element處理訊息
        /// </summary>
        /// <returns></returns>
        public virtual ICollection<string> GetMessages()
        {
            return ElementProcessingMessages;
        }
        /// <summary>
        /// 垃圾回收機制
        /// </summary>
        public void Dispose()
        {
            DBNormalFields.Clear();
            DBPrimaryKeyFields.Clear();

            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 清除所有欄位資料
        /// </summary>
        public void ClearWholeFieldValues()
        {
            DBPrimaryKeyFields.ForEach(x => { x.ResetValue(); });
            DBNormalFields.ForEach(x => { x.ResetValue(); });
            DBDatasets.Clear();
        }
        /// <summary>
        /// 清除欄位資料
        /// </summary>
        public void ClearFields()
        {
            DBPrimaryKeyFields.Clear();
            DBNormalFields.Clear();
        }

        /// <summary>
        /// 複製Element,只複製容器的內容
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            return new ElementAbstract(this);
        }
        /// <summary>
        /// 指定建立使用者服號及
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public ElementAbstract SetCreateUser(string createUser, string createDateTime)
        {
            Apply(new CommandFieldEvent.OnCreateDbUser()
            {
                CreateUser = createUser,
                CreateDateTime = createDateTime
            });
            return this;
        }
        /// <summary>
        /// 指定
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public ElementAbstract SetModifyUser(string modifiedUser, string modifiedDateTime)
        {
            Apply(new CommandFieldEvent.OnModifiedDbUser()
            {
                ModifiedUser = modifiedUser,
                ModifiedDateTime = modifiedDateTime
            });
            return this;
        }

        /// <summary>
        /// 事件驅動
        /// </summary>
        /// <param name="event"></param>
        protected override void When(object @event)
        {
            switch (@event)
            {
                case CommandFieldEvent.OnCreateDbUser e:
                    CreateUser = new TableFieldProperty()
                            .SetDbField("CreateUser", FieldType.ftString, false, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                            .UpdateDbFieldValues((e.CreateUser == "") ? "Teramed administrator" : e.CreateUser, "", null);

                    CreateDateTime = new TableFieldProperty()
                            .SetDbField("CreateDateTime", FieldType.ftString, false, false, true, false, FieldOperator.foAnd, OrderOperator.foNone);
                    if (e.CreateDateTime != "")
                        CreateDateTime.UpdateDbFieldValues(e.CreateDateTime, "", null);
                    State = ElementState.UserCreated;
                    break;
                case CommandFieldEvent.OnModifiedDbUser e:
                    ModifiedUser = new TableFieldProperty()
                            .SetDbField("ModifiedUser", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                            .UpdateDbFieldValues((e.ModifiedUser == "") ? "Teramed administrator" : e.ModifiedUser, "", null);
                    ModifiedDateTime = new TableFieldProperty()
                            .SetDbField("ModifiedDateTime", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone);
                    if (e.ModifiedDateTime != "")
                        ModifiedDateTime.UpdateDbFieldValues(e.ModifiedDateTime, "", null);
                    State = ElementState.UserModified;
                    break;
            }
            return;
        }
        /// <summary>
        /// 確認資料是否正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;
            switch (State)
            {
                case ElementState.UserCreated:
                    valid = (CreateUser.Value != string.Empty);
                    break;
                case ElementState.UserModified:
                    valid = (ModifiedUser.Value != string.Empty);
                    break;
            }
            if (!valid)
                throw new InvalidEntityStateException(this, $"Post-checks failed in state {State}");
        }
        #endregion

        public enum ElementState
        {
            UserCreated,
            UserModified
        }
    }
    #endregion

    #region ElementRepositoryHelper
    /// <summary>
    /// ElementAbstract工具物件
    /// </summary>
    public class TableElementHelper
    {
        /// <summary>
        /// 搜尋欄位
        /// </summary>
        /// <param name="element"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static ICommonFieldProperty FindField(IElementInterface element, string fieldName)
        {
            ICommonFieldProperty cmField = null;
            //先找主鍵欄位            
            cmField = element.DBPrimaryKeyFields.Find(field => field.FieldName == fieldName);
            if (cmField == null)
            {
                //若沒有找到,接著在找非主鍵欄位
                cmField = element.DBNormalFields.Find(field => field.FieldName == fieldName);
            }
            return cmField;
        }
        /// <summary>
        /// 搜尋主鍵欄位
        /// </summary>
        /// <param name="element"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static ICommonFieldProperty FindPrimaryKeyField(IElementInterface element, string fieldName)
        {
            return element.DBPrimaryKeyFields.Find(field => field.FieldName == fieldName);
        }
        /// <summary>
        /// 搜尋非主鍵欄位
        /// </summary>
        /// <param name="element"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static ICommonFieldProperty FindNormalKeyField(IElementInterface element, string fieldName)
        {
            return element.DBNormalFields.Find(field => field.FieldName == fieldName);
        }
        /// <summary>
        /// 移動非主鍵到主鍵欄位
        /// </summary>
        /// <param name="element"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static ICommonFieldProperty MoveNormalKeyToPrimaryKey(IElementInterface element, string fieldName)
        {
            ICommonFieldProperty field;
            try
            {
                field = FindNormalKeyField(element, fieldName);
                element.DBNormalFields.Remove(field);
                element.DBPrimaryKeyFields.Add(field);
            }
            catch (Exception)
            {
                return null;
            }
            return field;
        }
        /// <summary>
        /// 更新資料庫欄位資料
        /// </summary>
        /// <param name="element"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool UpdateFieldValue(IElementInterface element, string fieldName, string value)
        {
            try
            {
                ICommonFieldProperty cmField = null;
                //先找主鍵欄位            
                cmField = element.DBPrimaryKeyFields.Find(field => field.FieldName == fieldName);
                if (cmField == null)
                {
                    //若沒有找到,接著在找非主鍵欄位
                    cmField = element.DBNormalFields.Find(field => field.FieldName == fieldName);
                    if (cmField == null)
                        return false;
                }
                //MOD BY JB 20211025 改為强制資料判斷
                cmField.UpdateDbFieldValues(value, "", null);
                //cmField.Value = value;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 查詢表格
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IElementInterface FindTable(IElementInterface element, string tableName)
        {
            IElementInterface result = null;
            if (element == null)
                return result;

            if (element is not MasterDetailTable masterDetailTable)
                return result;

            result = masterDetailTable.DetailElements.Find(detailTable => detailTable.TableName == tableName);

            return result;
        }
        /// <summary>
        /// ADD 20210701 Oscar
        /// 從Dataset內搜尋欄位,而不是從欄位列表中去搜尋欄位
        /// </summary>
        /// <param name="element">資料物件</param>
        /// <param name="fieldName">要搜尋的欄位名稱</param>
        /// <param name="assignDatasetIndex">指定從第幾個dataset搜尋</param>
        /// <returns></returns>
        public static ICommonFieldProperty FindFieldFromDataset(IElementInterface element, string fieldName, int assignDatasetIndex)
        {
            ICommonFieldProperty cmField = null;

            if ((element.DBDatasets == null) || (!element.DBDatasets.Any()))
                return cmField;

            if (((assignDatasetIndex < 0)) || (assignDatasetIndex > element.DBDatasets.Count))
                return cmField;

            List<ICommonFieldProperty> lstFieldProperty = element.DBDatasets[assignDatasetIndex];
            return (lstFieldProperty.Find(field => field.FieldName == fieldName));
        }
    }
    #endregion    
}