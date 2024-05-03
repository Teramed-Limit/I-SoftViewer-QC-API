using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Converter
{
    #region DcmBufferConverter
    /// <summary>
    /// DICOM影像轉換
    /// </summary>
    public class DcmBufferConverter : IImageBufferConverter<DicomFile>
    {
        /// <summary>
        /// 建構
        /// </summary>
        public DcmBufferConverter()
        {
        }

        #region Fields
        /// <summary>
        /// 訊息結果
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// 結果
        /// </summary>
        public OpResult Result { get; internal set; }
        #endregion

        #region Methods
        /// <summary>
        /// Base64 to DicomFile
        /// </summary>
        /// <param name="b64String"></param>
        /// <returns></returns>
        public virtual DicomFile Base64BufferToImage(byte[] decodedByteArray)
        {
            Result = OpResult.OpSuccess;
            DicomFile dcmFile = null;
            try
            {
                // byte[] decodedByteArray = Convert.FromBase64String(Encoding.ASCII.GetString(b64String));
                using MemoryStream mmStream = new(decodedByteArray);
                mmStream.Position = 0;
                dcmFile = DicomFile.Open(mmStream, FileReadOption.ReadAll);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }
            return dcmFile;
        }
        /// <summary>
        /// DcmFile to Base64 byte[]
        /// </summary>
        /// <param name="dcmFile"></param>
        /// <returns></returns>
        public virtual byte[] ImageFileToBase64(DicomFile dcmFile)
        {
            Result = OpResult.OpSuccess;
            byte[] result = null;
            try
            {
                using MemoryStream mmStream = new();
                dcmFile.Save(mmStream);
                result = mmStream.ToArray();
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }
            return result;
        }
        #endregion
    }
    #endregion

    #region NoneDcmBufferConverter
    /// <summary>
    /// BMP,JPG,PNG 轉DICOM
    /// </summary>
    public class NoneDcmBufferConverter : DcmBufferConverter
    {
        /// <summary>
        /// 建構
        /// </summary>
        public NoneDcmBufferConverter()
        {
        }

        #region Mehtods
        /// <summary>
        /// Base64 to DicomFile
        /// </summary>
        /// <param name="b64String"></param>
        /// <returns></returns>
        public override DicomFile Base64BufferToImage(byte[] decodedByteArray)
        {
#pragma warning disable CA1416 // 驗證平台相容性
            Result = OpResult.OpSuccess;
            DicomFile result = null;
            try
            {
                //先將Base64轉成Bitmap格式
                // byte[] decodedByteArray = Convert.FromBase64String(Encoding.ASCII.GetString(b64String));
                // using MemoryStream mmStream = new(decodedByteArray);

                using MemoryStream mmStream = new(decodedByteArray);

                Image image = Image.FromStream(mmStream);
                Bitmap bitmap = new(image);
                //需要在將RGB轉成BGR的格式
                byte[] bytedata = GetNoneDcmPixelData(bitmap);
                if (bytedata == null)
                    throw new Exception("failed to GetNoneDcmPixelData");
                //產生Dicom Pixel Data                
                DicomDataset dataset = new();
                if (WritePixelDataInDataset(dataset, bytedata, bitmap.Width, bitmap.Height) == false)
                    throw new Exception("Failed to write pixel data in dataset");

                //先暫時產生一組隨機的UID,讓DICOM File可以正常產生              
                DicomUID studyUid = DicomUIDGenerator.GenerateDerivedFromUUID();
                DicomUID seriesUid = DicomUIDGenerator.GenerateDerivedFromUUID();
                DicomUID insUid = DicomUIDGenerator.GenerateDerivedFromUUID();

                dataset.Add(DicomTag.StudyInstanceUID, studyUid);
                dataset.Add(DicomTag.SeriesInstanceUID, seriesUid);
                dataset.Add(DicomTag.SOPInstanceUID, insUid);
                dataset.Add(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.7");

                result = new DicomFile(dataset);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }
            return result;
#pragma warning restore CA1416 // 驗證平台相容性
        }
        /// <summary>
        /// 取得None DICOM影像的像素資料
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        protected byte[] GetNoneDcmPixelData(Bitmap bitmap)
        {
            byte[] bytedata = null;
            try
            {
                BitmapData bmpdata = null;
#pragma warning disable CA1416 // 驗證平台相容性
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                bytedata = new byte[(uint)(bmpdata.Stride * bmpdata.Height)];

                //使用unsafe指標處理方式,加快處理速度
                unsafe
                {
                    int offset = 0;
                    byte* line = (byte*)bmpdata.Scan0;
                    for (int y = 0; y < bmpdata.Height; y++)
                    {
                        for (int x = 0; x < bmpdata.Width; x++)
                        {
                            byte* pos = line + x * 3;
                            bytedata[offset++] = pos[2];
                            bytedata[offset++] = pos[1];
                            bytedata[offset++] = pos[0];
                        }
                        line += bmpdata.Stride;
                    }
                }
                if (bytedata != null)
                    bitmap.UnlockBits(bmpdata);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return null;
            }
            return bytedata;
#pragma warning restore CA1416 // 驗證平台相容性
        }
        /// <summary>
        /// 將Buffer寫入Pixel Data
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="buffer"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected bool WritePixelDataInDataset(DicomDataset dataset, byte[] buffer, int width, int height)
        {
            try
            {
                DicomOperatorHelper dcmOpHelper = new();
                dcmOpHelper.WriteDicomValueInDataset(dataset, DicomTag.PhotometricInterpretation, "RGB", true);
                dcmOpHelper.WriteDicomValueInDataset(dataset, DicomTag.Rows, Convert.ToString(height), true);
                dcmOpHelper.WriteDicomValueInDataset(dataset, DicomTag.Columns, Convert.ToString(width), true);
                dcmOpHelper.WriteDicomValueInDataset(dataset, DicomTag.BitsAllocated, "8", true);

                DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
                pixelData.BitsStored = 8;
                pixelData.SamplesPerPixel = 3;
                pixelData.HighBit = 7;
                pixelData.PixelRepresentation = 0;
                pixelData.PlanarConfiguration = 0;

                //將影像寫入到DicomDataset之中
                MemoryByteBuffer byteBuffer = new(buffer);
                pixelData.AddFrame(byteBuffer);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }
            return true;
        }
        #endregion
    }
    #endregion
}
