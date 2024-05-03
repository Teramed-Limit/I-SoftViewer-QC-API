using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibUnitTest.ToolFuncs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;
using static ISoftViewerLibrary.Models.DTOs.DICOMConfig.V1;
using static ISoftViewerLibrary.Models.DTOs.Permissions.V1;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class DTOsTest
    {
        [TestMethod]
        public void T1_UserRole_ThrowException()
        {
            Assert.ThrowsException<Exception>(() => new UserRole("", ""));             
        }

        [TestMethod]
        public void T2_UserRole_Clone_IsTrue()
        {
            UserRole userRole = new("Name", "Description", new List<OpFunctionName>() 
                                    { new OpFunctionName("QC1", "QCDescription1"),
                                      new OpFunctionName("QC2", "QCDescription2")});            
            
            UserRole userRole2 = userRole.Clone() as UserRole;
            Assert.IsTrue(ClassComparer<UserRole, OpFunctionName>.Comparer(userRole, userRole2));            
        }

        [TestMethod]
        public void T3_UserAccount_ThrowException()
        {
            Assert.ThrowsException<Exception>(() => new UserAccount("", ""));
        }

        [TestMethod]
        public void T4_UserAccount_Clone_AreEqual()
        {
            List<UserRole> userRoles = new()
            {
                new UserRole("Role1", "Description1", new List<OpFunctionName>()
                                    { new OpFunctionName("QC1", "QCDescription1"),
                                      new OpFunctionName("QC2", "QCDescription2")}),
                new UserRole("Role2", "Description2", new List<OpFunctionName>()
                                    { new OpFunctionName("QC1", "QCDescription1"),
                                      new OpFunctionName("QC2", "QCDescription2")})
            };
            UserAccount account1 = new("user", "user", "20211015", false, userRoles);
            UserAccount account2 = account1.Clone() as UserAccount;

            Assert.IsTrue(ClassComparer<UserAccount, UserRole>.Comparer(account1, account2));            
        }

        [TestMethod]
        public void T5_OpFunctionName_ThrowException()
        {
            Assert.ThrowsException<Exception>(() => new OpFunctionName("", ""));
        }

        [TestMethod]
        public void T6_DICOMProvider_ThrowException()
        {
            Assert.ThrowsException<Exception>(() => new DICOMProvider("", "", 0, DcmServiceType.dstStoreSCP));
        }

        [TestMethod]
        public void T7_DICOMProvider_IsFalse()
        {
            DICOMProvider provider1 = new("Provider1", "AETitle1", 104, DcmServiceType.dstStoreSCP);
            DICOMProvider provider2 = provider1.Clone() as DICOMProvider;                       

            provider2.DicomServiceType = DcmServiceType.dstWorklistSCP;            
            Assert.IsFalse(ClassComparer<DICOMProvider,object>.Comparer(provider1, provider2));            
        }

        [TestMethod]
        public void T8_DICOMScu_ThrowException()
        {
            Assert.ThrowsException<Exception>(() => new DICOMScu("", "", 0, "", "", "", 0, ""));
        }

        //[TestMethod]
        //public void T9_IodData_AreEqual()
        //{
        //    string trueValue = @"{""PatientId"":""TestPatientID"",""PatientsName"":""TestPatientName"",""PatientsSex"":"""",";
        //    trueValue += @"""PatientsBirthDate"":"""",""PatientsBirthTime"":"""",""OtherPatientNames"":"""",""OtherPatientId"":""""}";

        //    PatientData patientIE = new("TestPatientID", "TestPatientName");            
        //    Assert.AreEqual(trueValue, patientIE.ToString());            
        //}        
    }
}
