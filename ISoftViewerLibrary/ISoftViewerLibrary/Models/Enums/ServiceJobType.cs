namespace ISoftViewerLibrary.Models.Enums
{
    public enum ServiceJobType
    {
        sjtCStored = 0,
        stjSaveToJpegThumbnail = 1,
        stjRouting = 3,
        stjRoutingAfterDelFiles = 4,
        stjDcmTagCorrection = 5,
        stjImageProcessing = 6,
        stjCuhkCustomized = 7,
        stjCStoredVideo = 8,
        stjDcmVideoConvertToMp4 = 9,
        stjDicomImageVideComposite = 10,
        stjDcmImagAndVideoThumbnailJob = 11
    }
}