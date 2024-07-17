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

        public partial class LoaiHoaDon
        {
            public const string TienDien = "TD";
            public const string GTGT = "GT";
            public const string PhanKhang = "VC";
            public static string GetString(string id)
            {
                switch (id)
                {
                    case TienDien: return "Tiền điện";
                    case GTGT: return "Dịch vụ khác";
                    case PhanKhang: return "Phản kháng";
                    default: return "Không xác định";
                }
            }
            public static List<string> ListId => typeof(LoaiHoaDon).GetAllPublicConstantValues<string>();
            public static List<KeyValuePair<string, string>> ListKeyValue => ListId.Select(p => new KeyValuePair<string, string>(p, GetString(p))).ToList();
        }

        public partial class D_TinhChatHoaDon
        {
            public const string PhatSinh = "PS";
            public const string HuyBo = "HB";
            public const string LapLai = "LL";
            public const string TruyThu = "TT";
            public const string ThoaiHoan = "TH";
            public const string HuyBoKhongLapLai = "HBK";
            public static string GetString(string id)
            {
                switch (id)
                {
                    case PhatSinh: return "Phát sinh";
                    case HuyBo: return "Hủy bỏ";
                    case LapLai: return "Lập lại";
                    case TruyThu: return "Truy thu";
                    case ThoaiHoan: return "Thoái hoàn";
                    case HuyBoKhongLapLai: return "Hủy bỏ không lập lại";
                    default: return "Không xác định";
                }
            }
            public static List<string> ListId => typeof(D_TinhChatHoaDon).GetAllPublicConstantValues<string>();
            public static List<KeyValuePair<string, string>> ListKeyValue => ListId.Select(p => new KeyValuePair<string, string>(p, GetString(p))).ToList();
            
        }

        [Flags]
        public enum StatusTrackDebt
        {
            // giá trị trường Status trong bảng Liabilities_TrackDebt
            Unpaid = 0, // chưa thanh toán
            Paid = 1, //Đã thanh toán
            Cancel = 2,//Hóa đơn đã hủy bỏ, chưa có lập lai
            Restore = 3,// hóa đơn hủy bỏ đã được khôi phục bằng hóa đơn khác
        }

        [Flags]
        public enum StatusCalendarOfSaveIndex
        {
            CreateCalendar = 1, // lập lịch ghi chỉ số
            Gcs = 3, //GCS
            ConfirmGcs = 5,//Xác nhận số liệu ghi chỉ số
            Bill = 7,// tính hóa đơn
            ConfirmData = 9, //Xác nhận số liệu , xác nhận để chuyển hóa đơn tính ra sang dạng xml để chuẩn bị ký hóa đơn điện tử
            SigningBill = 10, //Trạng thái tồn tại trong thời gian rất ngắn, khi đang ký. Nếu select thấy giá trị này có nghĩa là sổ đang ký bị lỗi
            SignedandReleased = 11// Đã ký hóa đơn điện tử, chuyển sang công nợ, phát hành hóa đơn
        }
    }
}