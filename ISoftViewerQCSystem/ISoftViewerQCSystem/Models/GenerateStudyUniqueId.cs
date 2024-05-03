using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;

namespace ISoftViewerQCSystem.Models
{
    public class GenerateStudyUniqueId
    {
        public string AccessionNumber { get; set; }

        public string StudyInstanceUID { get; set; }
    }
}
