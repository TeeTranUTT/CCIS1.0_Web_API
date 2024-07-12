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

        public partial class LoaiChiSo
        {
            public const string DUP = "DUP";
            public const string DDK = "DDK";
            public const string DDN = "DDN";
            public const string CCS = "CCS";
            public const string CSC = "CSC";
            public static string GetString(string id)
            {
                switch (id)
                {
                    case DUP: return "CS treo";
                    case DDK: return "Chỉ số định kỳ";
                    case DDN: return "CS tháo";
                    case CCS: return "Đổi giá khách hàng";
                    case CSC: return "Đổi giá nhà nước";
                    default: return "Không xác định";
                }
            }
            public static List<string> ListId => typeof(LoaiChiSo).GetAllPublicConstantValues<string>();
            public static List<KeyValuePair<string, string>> ListKeyValue => ListId.Select(p => new KeyValuePair<string, string>(p, GetString(p))).ToList();
        }

        public class BoChiSo
        {
            public const string KT = "KT";
            public const string BT = "BT";
            public const string CD = "CD";
            public const string TD = "TD";
            public const string VC = "VC";
            public const string SG = "SG";
            public static string GetString(string id)
            {
                switch (id)
                {
                    case KT: return "Toàn thời gian";
                    case BT: return "Bình thường";
                    case CD: return "Cao điểm";
                    case TD: return "Thấp điểm";
                    case VC: return "Vô Công";
                    case SG: return "Hữu công";
                    default: return "Không xác định";
                }
            }
            public static List<string> ListId => typeof(BoChiSo).GetAllPublicConstantValues<string>();
            public static List<KeyValuePair<string, string>> ListKeyValue => ListId.Select(p => new KeyValuePair<string, string>(p, GetString(p))).ToList();

            public static List<string> ListBcsHuuCong = new List<string>() { BT, CD, TD, };
            public static List<string> ListBcsVoCong = new List<string>() { VC, };
        }

        public partial class NganhNghe
        {
            public const string SHBT = "SHBT";
            public const string CQBV = "CQBV";
            public const string CQHC = "CQHC";
            public const string KDDV = "KDDV";
            public const string SHBD = "SHBD";
            public const string SHTM = "SHTM";
            public const string SXBT = "SXBT";
            public static string GetString(string id)
            {
                switch (id)
                {
                    case SHBT: return "SHBT";
                    case CQBV: return "CQBV";
                    case CQHC: return "CQHC";
                    case KDDV: return "KDDV";
                    case SHBD: return "SHBD";
                    case SHTM: return "SHTM";
                    case SXBT: return "SXBT";

                    default: return "Không xác định";
                }
            }
            public static List<string> ListId => typeof(NganhNghe).GetAllPublicConstantValues<string>();
            public static List<KeyValuePair<string, string>> ListKeyValue => ListId.Select(p => new KeyValuePair<string, string>(p, GetString(p))).ToList();
        }
    }
}