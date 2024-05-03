using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace ISoftViewerLibrary.Utils
{
    public class TableField
    {
        public List<PairDatas> PrimaryFields { get; set; }
        public List<PairDatas> NormalFields { get; set; }
    }

    public class ObjectToPairDatas
    {
        public static TableField Convert(object obj, List<string> primaryKeys, string identityPrimaryKey = null)
        {
            // var skipFieldList = new List<string>
            // {
            //     "CreateUser",
            //     "CreateDateTime",
            //     "ModifiedDateTime",
            //     "ModifiedUser"
            // };

            // 如果PrimaryKey是識別規格，則忽略,因為資料庫會自動增加
            // if (identityPrimaryKey != null) skipFieldList.Add(identityPrimaryKey);

            var primaryFields = new List<PairDatas>();
            var normalFields = new List<PairDatas>();

            obj.GetType().GetProperties();
            foreach(var prop in obj.GetType().GetProperties())
            {
                var pairDatas = new PairDatas();
                pairDatas.Name = prop.Name;
                pairDatas.Value = prop.GetValue(obj)?.ToString();

                if(pairDatas.Name is "DataRetrievalFuncs" or "DataWritingActions")
                    continue;
                
                // if(skipFieldList.Any(x => skipFieldList.Contains(pairDatas.Name)))
                //     continue;

                switch (prop.PropertyType.Name)
                {
                    case "String":
                        pairDatas.Type = FieldType.ftString;
                        break;
                    case "Int32":
                        pairDatas.Type = FieldType.ftInt;
                        break;
                    case "DateTime":
                        pairDatas.Type = FieldType.ftDateTime;
                        break;
                    default:
                        pairDatas.Type = FieldType.ftString;
                        break;
                }

                if(primaryKeys.Contains(pairDatas.Name))
                    primaryFields.Add(pairDatas);
                else
                    normalFields.Add(pairDatas);
            }

            return new TableField() { PrimaryFields = primaryFields, NormalFields = normalFields };
        }
    }
}
