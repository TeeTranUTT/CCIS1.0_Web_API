using ES.CCIS.Host.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.EnumMethods
{
    public partial class EnumMethod
    {
        public partial class TrangThai
        {
            public const int VoHieu = 0;
            public const int KichHoat = 1;
            public static string GetString(int? id)
            {
                if (id == null)
                {
                    return null;
                }
                else
                {
                    return GetString(KichHoat);
                }
                
            }
            public static List<int> ListId => typeof(TrangThai).GetAllPublicConstantValues<int>();
            public static List<KeyValuePair<int, string>> ListKeyValue => ListId.Select(p => new KeyValuePair<int, string>(p, GetString(p))).ToList();

            public const bool Deactive = false;
            public const bool Active = true;
            public static string GetString(bool? id)
            {
                switch (id)
                {
                    case false: return "Vô hiệu";
                    case true: return "Kích hoạt";
                    default: return "Không xác định";
                }
            }
            public static List<bool> ListBoolean => typeof(TrangThai).GetAllPublicConstantValues<bool>();
            public static List<KeyValuePair<bool, string>> ListKeyValueBoolean => ListBoolean.Select(p => new KeyValuePair<bool, string>(p, GetString(p))).ToList();

        }
    }
}