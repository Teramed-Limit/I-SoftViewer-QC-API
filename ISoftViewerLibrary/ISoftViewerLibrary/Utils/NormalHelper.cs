using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Utils
{
    #region NormalHelper
    /// <summary>
    /// 常用的助手物件
    /// </summary>
    public class NormalHelper
    {
        /// <summary>
        /// 取得Enum Description
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }
            return value.ToString();
        }
        /// <summary>
        /// 依照string去轉換列舉值
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="description"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static TEnum GetEnumByDescription<TEnum>(string description, bool ignoreCase = false)            
            where TEnum : Enum
        {            
            //取得該列舉的所有欄位
            foreach (var item in typeof(TEnum).GetFields())
            {                
                if (Attribute.GetCustomAttribute(item, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    //比對字串和Description是否相同
                    if (string.Equals(attribute.Description, description, 
                        ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        return (TEnum)item.GetValue(null);
                }
            }            
            throw new ArgumentException($"Enum item with description \"{description}\" could not be found", nameof(description));
        }
        /// <summary>
        /// 用文字說明去返推列舉值
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="description"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetEnumByDescription<TEnum>(string description, bool ignoreCase, out TEnum result)
            where TEnum : Enum
        {
            try
            {                
                result = GetEnumByDescription<TEnum>(description, ignoreCase);
                return true;
            }
            catch (ArgumentException)
            {                
                result = default;
                return false;
            }
        }
    }
    #endregion
}
