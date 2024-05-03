using Dicom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IDcmBufferConverter
    /// <summary>
    /// Base64和DicomFile資料互轉
    /// </summary>
    public interface IImageBufferConverter<ImageObject> : IOpMessage
    {
        /// <summary>
        /// Base64 to Image
        /// </summary>
        /// <param name="b64String"></param>
        /// <returns></returns>
        ImageObject Base64BufferToImage(byte[] b64String);
        /// <summary>
        /// Image to Base64 byte[]
        /// </summary>
        /// <param name="dcmFile"></param>
        /// <returns></returns>
        byte[] ImageFileToBase64(ImageObject dcmFile);        
    }
    #endregion
}
