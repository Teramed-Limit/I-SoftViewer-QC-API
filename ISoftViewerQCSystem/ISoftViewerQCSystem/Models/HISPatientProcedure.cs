using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ISoftViewerQCSystem.HIS
{
    public class HISPatientEpisode
    {
        public string CumcNo { get; set; }
        public string EpisodeNo { get; set; }
        public string Dept { get; set; }
        public string BedNo { get; set; }
        public string AdmissionDate { get; set; }
    }

    public class HISPatientProcedure
    {
        public string EpisodeNo { get; set; }
        public string ProcedureID { get; set; }
        public string ProcedureTeam { get; set; }
        public string AccessionNumber { get; set; }
        public string StudyInstanceUID { get; set; }
        public string ProcedureCode { get; set; }
        public string ProcedureDesc { get; set; }
        public string ProcedureDatetime { get; set; }
        public string SedationTypeCode { get; set; }
        public string SedationType { get; set; }
        public string StartDate { get; set; }
        public string Dept { get; set; }
        public string EndDate { get; set; }
    }

    public class HISPatientDoctor
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class HISPatientData
    {
        public string TimeStartHour { get; set; }
        public string TimeStartMin { get; set; }
        public string TimeEndHour { get; set; }
        public string TimeEndMin { get; set; }
        public string CumcNo { get; set; }
        public string SurName { get; set; }
        public string GivenName { get; set; }
        public string OtherName { get; set; }
        public string NameChinese { get; set; }
        public string Birthdate { get; set; }
        public string Sex { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public HISPatientEpisode HISPatientEpisode { get; set; }
        public List<HISPatientProcedure> LstHISPatientProcedure { get; set; }
        public List<HISPatientDoctor> LstHISPatientDoctor { get; set; }
        public List<HISPatientDoctor> ChiefDoctorList { get; set; }
        public List<HISPatientDoctor> EndoDoctorList { get; set; }
        public List<HISPatientDoctor> AnesDoctorList { get; set; }
    }
}