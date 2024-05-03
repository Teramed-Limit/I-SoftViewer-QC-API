using System.Linq;

namespace ISoftViewerLibrary.Utils
{
    public class TrimObjectHelper
    {
        public static void Trim(object obj)
        {
            var stringProperties = obj.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(string));

            foreach (var stringProperty in stringProperties)
            {
                var currentValue = (string)stringProperty.GetValue(obj, null);
                if(currentValue == null) continue;
                stringProperty.SetValue(obj, currentValue.Trim(), null);
            }
        }
    }
}