using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibUnitTest.ToolFuncs
{
    //物件比較器
    public class ClassComparer<T1,T2>
    {
        public static bool Comparer(T1 obj1, T1 obj2)
        {
            Type type_1 = obj1.GetType();
            foreach (PropertyInfo property in type_1.GetProperties())
            {
                string name = property.Name;
                                
                Object prop1 = property.GetValue(obj1, null);
                Type type = prop1.GetType();
                Type type1 = typeof(List<T2>);
                if (prop1 == null)
                {
                    return false;
                }                    
                else if (prop1.GetType() == typeof(List<T2>))
                {
                    Object prop2 = obj2.GetType().GetProperty(name)?.GetValue(obj2, null);
                    if (prop2 == null || prop2.GetType() != typeof(List<T2>))                    
                        return false;

                    List<T2> enumerable2 = prop2 as List<T2>;
                    if (prop1 is List<T2> enumerable1)
                    {
                        foreach (var listitem in enumerable1)
                        {
                            T2 result = enumerable2.Find(x => x.Equals(listitem));
                            if (result == null)
                                return false;
                        }
                    }                    
                }
                else
                {
                    string value_1 = prop1.ToString();
                    string value_2 = obj2.GetType().GetProperty(name)?.GetValue(obj2, null)?.ToString();
                    if (value_1 != value_2)
                        return false;
                }
            }
            return true;
        }
    }
}
