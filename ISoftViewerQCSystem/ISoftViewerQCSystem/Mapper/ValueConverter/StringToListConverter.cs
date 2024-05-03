using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace ISoftViewerQCSystem.Mapper.ValueConverter
{
    public class StringToListConverter : IValueConverter<string, List<string>>
    {
        public List<string> Convert(string source, ResolutionContext context)
        {
            if(string.IsNullOrEmpty(source))
            {
                return new List<string>();
            }

            return source.Split(",").ToList();
        }
    }
}