using System.Collections.Generic;
using ISoftViewerLibrary.Models.Interfaces;

namespace ISoftViewerLibrary.Models.ValueObjects
{
    public class TablePairData
    {
        public TablePairData()
        {
            TableName = "";
            Type = TableType.ftMaster;
        }

        public TablePairData(string name, TableType type)
        {
            // PairDataList = PairDataList;
            TableName = name;
            Type = type;
        }

        public string TableName { get; set; }

        public TableType Type { get; set; }
        public List<PairDatas> PairDataList { get; set; }
    }
}