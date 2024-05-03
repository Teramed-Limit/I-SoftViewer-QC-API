using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;

namespace ISoftViewerLibUnitTest.ToolFuncs
{
    public class ToolFunc
    {
        public static CreateAndModifyStudy<ImageBufferAndData> GetDicomIODs()
        {
            ImageBufferAndData bufferData = new("1.3.6.1.4.1.57995.1.3.1258350856.19144.1635836821.983", "1.3.6.1.4.1.57995.1.3.1258350856.19144.1635836821.982", "1.2.840.10008.5.1.4.1.1.7");
            bufferData.Type = BufferType.btDcm;

            CreateAndModifyStudy<ImageBufferAndData> createStudy = new()
            {
                PatientInfo = new("NO0001", "TestPatientName"),
                StudyInfo = new List<StudyData>() { new("1.3.6.1.4.1.57995.1.2.1258350856.19144.1635836821.981", "TestPatientID") },
                SeriesInfo = new List<SeriesData>() { new("1.3.6.1.4.1.57995.1.3.1258350856.19144.1635836821.982", "1.3.6.1.4.1.57995.1.2.1258350856.19144.1635836821.981") },
            };
            createStudy.ImageInfos.Add(bufferData);

            return createStudy;
        }
    }
}
