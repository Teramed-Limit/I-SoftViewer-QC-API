using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;

namespace ISoftViewerQCSystem.Applications
{
    /// <summary>
    /// 香港中文大學客製化服務
    /// 這邊會直接操作,因為客製化的內容應該不多,所以就不再細分到單獨的服務
    /// </summary>
    public class CuhkCustomizeSrvice
    {
        #region Fields
        /// <summary>
        /// 資料庫查詢服務
        /// </summary>
        protected DbQueriesService<CustomizeTable> DbQryService;
        /// <summary>
        /// 資料庫更新服務
        /// </summary>
        protected DbCommandService<CustomizeTable> DbCmdService;
        #endregion

        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        /// <param name="dbCmdService"></param>
        public CuhkCustomizeSrvice(DbQueriesService<CustomizeTable> dbQryService, DbCommandService<CustomizeTable> dbCmdService)
        {
            DbQryService = dbQryService;
            DbCmdService = dbCmdService;
        }

        /// <summary>
        /// 執行
        /// </summary>
        public async Task<bool> Execute(object command)
        {
            bool result = true;
            try
            {
                switch (command)
                {
                    //目前直接拿Merge參數來更新檢查報告(參數都一樣,應該也不會變)
                    case MergeStudyParameter cmd:
                        {
                            if (await IsStudyHasReport(cmd.FromStudyUID))
                                result = await UpdateReportingUID(cmd);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw new Exception("CuhkcustomizeService execute failed : " + ex.Message);
            }
            return result;
        }

        /// <summary>
        /// 該筆檢查是否有報告
        /// </summary>
        /// <param name="studyInstanceUID"></param>
        /// <returns></returns>
        protected Task<bool> IsStudyHasReport(string studyInstanceUID)
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUID))
                return Task.FromResult(true);

            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = studyInstanceUID } },
            };

            var table = DbQryService.BuildTable("DicomReporting", pkeys, new List<PairDatas>()).GetData();

            return Task.FromResult(table.DBDatasets.Any());
        }

        /// <summary>
        /// 更新報告UID
        /// </summary>
        /// <param name="msParameter"></param>
        /// <returns></returns>
        protected Task<bool> UpdateReportingUID(MergeStudyParameter msParameter)
        {
            bool result = true;
            try
            {
                //先檢查參數
             if (msParameter==null)
                    return Task.FromResult(result=false);
                result &= (!string.IsNullOrWhiteSpace(msParameter.ModifyUser));
                result &= (!string.IsNullOrWhiteSpace(msParameter.FromStudyUID));
                result &= (!string.IsNullOrWhiteSpace(msParameter.ToStudyUID));

                if(!result)
                    Task.FromResult(result);

                //準備欄位與更新資料庫
                CustomizeTable reportTable = new(msParameter.ModifyUser, "DicomReporting");
                ICommonFieldProperty oriUidField = new TableFieldProperty()
                                                        .SetDbField("StudyInstanceUID", FieldType.ftString, false, false, true, false, FieldOperator.foAnd, OrderOperator.foNone);
                oriUidField.UpdateDbFieldValues(msParameter.FromStudyUID, "", null);
                reportTable.DBPrimaryKeyFields.Add(oriUidField);
                ICommonFieldProperty newUidField = new TableFieldProperty()
                                                        .SetDbField("StudyInstanceUID", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone);
                newUidField.UpdateDbFieldValues(msParameter.ToStudyUID, "", null);
                reportTable.DBNormalFields.Add(newUidField);
                var userField = reportTable.DBNormalFields.Find(x => x.FieldName == "ModifiedUser");
                if (userField is ICommonFieldProperty modifyUserField)
                    modifyUserField.UpdateDbFieldValues(msParameter.ModifyUser, "", null);

                DbCmdService.TableElement = reportTable;

                return DbCmdService.AddOrUpdate(true);
            }
            catch(Exception ex)
            {
                throw new Exception("UpdateReportingUID failed : " + ex.Message);
            }
        }
    }
}
