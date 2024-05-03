using System.Collections.Generic;
using System.Text.Json;
using AutoMapper;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerQCSystem.Dtos;
using ISoftViewerQCSystem.HIS;
using ISoftViewerQCSystem.Mapper.ValueConverter;
using ISoftViewerQCSystem.utils;
using Microsoft.Extensions.Configuration;

namespace ISoftViewerQCSystem.Mapper
{
    public class ServiceMappings : Profile
    {
        public ServiceMappings(IConfiguration configuration)
        {
            CreateMap<LoginUserData, LoginUserDataDto>()
                .ForMember(d => d.RoleList, s => s.ConvertUsing(new StringToListConverter()));

            CreateMap<LoginUserDataDto, LoginUserData>()
                .ForMember(d => d.RoleList, s => s.MapFrom(x => string.Join(",", x.RoleList.ToArray())));

            CreateMap<HISPatientData, HISDataDto>()
                .ForMember(d => d.NameEng, s => s.MapFrom(x => x.OtherName))
                .ForMember(d => d.Dept, s => s.MapFrom(x => x.HISPatientEpisode.Dept))
                .ForMember(d => d.AdmissionDate, s => s.MapFrom(x => x.HISPatientEpisode.AdmissionDate))
                .ForMember(d => d.EpisodeNo, s => s.MapFrom(x => x.HISPatientEpisode.EpisodeNo))
                .ForMember(d => d.Birthdate, s => s.MapFrom(x => x.Birthdate.Substring(0, 8)));

            CreateMap<SearchImagePathView, SearchImagePathViewDto>()
                // .ForMember(d => d.JpgPath, s => s.MapFrom(x =>
                //     configuration.GetSection("VirtualFilePath").Value + FileUtils.ConvertToWebPath(x.FilePath, ".jpg")))
                .ForMember(d => d.HttpFilePath, s => s.MapFrom(x =>
                    configuration.GetSection("VirtualFilePath").Value + x.HttpFilePath));

            CreateMap<DicomImageData, DicomImageDataDto>()
                .ForMember(d => d.JpgPath, s => s.MapFrom(x =>
                    configuration.GetSection("VirtualFilePath").Value + FileUtils.ConvertToWebPath(x.FilePath, ".jpg")))
                .ForMember(d => d.DcmPath, s => s.MapFrom(x =>
                    configuration.GetSection("VirtualFilePath").Value +
                    FileUtils.ConvertToWebPath(x.FilePath, ".dcm")));
        }
    }
}