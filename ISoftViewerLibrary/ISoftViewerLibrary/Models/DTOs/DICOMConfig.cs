using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs
{
    public static class DICOMConfig
    {
        public static class V1
        {
            #region DICOMProvider
            /// <summary>
            /// DICOM服務類型, MOD BY JB 增加QRModalSCP
            /// </summary>
            public enum DcmServiceType { dstStoreSCP = 1, dstWorklistSCP = 2, QRModalSCP = 3 };
            /// <summary>
            /// DICOM服務提供者
            /// </summary>
            public class DICOMProvider : JsonDatasetBase, ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                public DICOMProvider()
                {
                    Name = "";                    
                    AETitle = "";
                    Port = 104;
                    DicomServiceType = DcmServiceType.dstStoreSCP;
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="name"></param>
                /// <param name="aeTitle"></param>
                /// <param name="port"></param>
                /// <param name="dicomServiceType"></param>
                public DICOMProvider(string name, string aeTitle, int port, DcmServiceType dicomServiceType)
                {
                    if (name == "" || aeTitle == "")
                        throw new Exception("Name and AETitle cannot be empty");
                    Name = name;
                    AETitle = aeTitle;
                    Port = port;
                    DicomServiceType = dicomServiceType;
                }
                #region Fields
                /// <summary>
                /// 名稱
                /// </summary>
                [Required]
                public string Name { get; set; }
                /// <summary>
                /// DICOM Application Entity
                /// </summary>
                [Required]
                public string AETitle { get; set; }
                /// <summary>
                /// 服務埠號
                /// </summary>
                [Required]
                public int Port { get; set; }
                /// <summary>
                /// DICOM服務型態
                /// </summary>
                [Required]
                public DcmServiceType DicomServiceType { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 複製副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new DICOMProvider(Name, AETitle, Port, DicomServiceType);
                }                
                #endregion
            }
            #endregion

            #region DICOMScu
            /// <summary>
            /// DICOM SUC設置
            /// </summary>
            public class DICOMScu : ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                public DICOMScu()
                {
                    Name = "";
                    AETitle = "";
                    PortName = 0;
                    RemoteAETitle = "";
                    Description = "";
                    WorklistReturnKeys = "";
                    WorklistQueryPattern = 0;
                    Department = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="name"></param>
                /// <param name="aETitle"></param>
                /// <param name="portName"></param>
                /// <param name="remoteAETitle"></param>
                /// <param name="description"></param>
                /// <param name="worklistReturnKeys"></param>
                /// <param name="worklistQueryPattern"></param>
                /// <param name="department"></param>
                public DICOMScu(string name, string aeTitle, int portName, string remoteAETitle, 
                    string description, string worklistReturnKeys, int worklistQueryPattern, string department)
                {
                    if (name == "" || aeTitle == "" || remoteAETitle == "")
                        throw new Exception("Name, AETitle and cannot be empty");

                    Name = name;
                    AETitle = aeTitle;
                    PortName = portName;
                    RemoteAETitle = remoteAETitle;
                    Description = description;
                    WorklistReturnKeys = worklistReturnKeys;
                    WorklistQueryPattern = worklistQueryPattern;
                    Department = department;
                }
                #region Fields
                /// <summary>
                /// 名稱
                /// </summary>
                [Required]
                public string Name { get; set; }
                /// <summary>
                /// Local AE Title
                /// </summary>
                [Required]
                public string AETitle { get; set; }
                /// <summary>
                /// Peer remote port
                /// </summary>
                [Required]
                public int PortName { get; set; }
                /// <summary>
                /// peer remote service title
                /// </summary>
                [Required]
                public string RemoteAETitle { get; set; }
                /// <summary>
                /// 說明備註
                /// </summary>
                [Required]
                public string Description { get; set; }
                /// <summary>
                /// Worklist查詢條件
                /// </summary>
                [Required]
                public string WorklistMatchKeys { get; set; }
                /// <summary>
                /// Worklist回覆條件
                /// </summary>                
                public string WorklistReturnKeys { get; set; }
                /// <summary>
                /// Worklist查詢型態
                /// </summary>
                public int WorklistQueryPattern { get; set; }
                /// <summary>
                /// 部門
                /// </summary>
                public string Department { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new DICOMScu(Name, AETitle, PortName, RemoteAETitle, Description, WorklistReturnKeys, WorklistQueryPattern, Department);
                }
                #endregion
            }
            #endregion
        }
    }
}
