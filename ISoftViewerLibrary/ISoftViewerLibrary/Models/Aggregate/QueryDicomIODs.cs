using System;
using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.ValueObjects;

namespace ISoftViewerLibrary.Models.Aggregate
{
    /// <summary>
    ///     DICOM 4層物件
    /// </summary>
    public class QueryDicomIODs : DicomIODs
    {
        #region Methods

        protected override void EnsureValidState()
        {
            var isEmptyEntity = true;
            switch (State)
            {
                case DicomIODsState.PatientCreated:
                    break;
                case DicomIODsState.StudyCreated:
                    break;
                case DicomIODsState.SeriesCreated:
                    var sePair = Series.Last().Value.First();
                    var properties = sePair.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(sePair);
                        if(property.Name == "OtherTags" && (value as List<DcmString>).Any())
                        {
                            isEmptyEntity = false;
                            break;
                        }

                        if(string.IsNullOrEmpty(value as string)) continue;

                        isEmptyEntity = false;
                        break;
                    }

                    if(isEmptyEntity) Series.Clear();
                    break;
                case DicomIODsState.ImageCreated:
                    break;
                case DicomIODsState.ScheduledCreated:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}