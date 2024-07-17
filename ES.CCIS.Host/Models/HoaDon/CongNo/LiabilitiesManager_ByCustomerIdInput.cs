using CCIS_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.HoaDon.CongNo
{
    public class LiabilitiesManager_ByCustomerIdInput
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; }
        public int BillId { get; set; }
        public string NgayCham { get; set; }
    }

    public class SaveLiabilities_TrackDebtInput
    {
        public int CustomerId { get; set; }
        public string NameMoney { get; set; }
        public int Bill { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class PaymentTrackDeptModel
    {
        public int CustomerId { get; set; }
        public string namemoney { get; set; }
        public int Bill { get; set; }
        public DateTime paymentDate { get; set; }
        public string PaymentMethodsCode { get; set; }
    }

    public class SaveLiabilities_TrackDebt_TaxInvoiceInput
    {
        public int CustomerId { get; set; }
        public int TaxInvoiceId { get; set; }
        public string NameMoney { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class LiabilitiesManager_TaxInvoiceInput
    {
        public int CustomerId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string CustomerCode { get; set; }
        public int TaxInvoiceId { get; set; }
    }

    public class DebtCancellationModel
    {
        public List<Liabilities_TrackDebtModel> liabilities_TrackDebts { get; set; }
        public int TongTien { get; set; }
        public int TongKH { get; set; }
    }

    public class Sms_postInput
    {
        public string[] ListBillId { get; set; }
        public int SmsTypeId { get; set; }
        public string NoiDung { get; set; }
    }

    public class LiabilitiesManagerByInput
    {
        public List<decimal> ListBillId { get; set; }
        public string Term { get; set; }
        public string SaveDate { get; set; }
        public string FigureBookId { get; set; }
    }

    public class Move_TrackDebtInput
    {
        public int FigurebookId { get; set; }
        public int Term { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public bool MoveDept { get; set; }
    }

    public class Move_TrackDebtListInput
    {
        public Category_FigureBookModel[] LstFigurebook { get; set; }
        public DateTime ConfirmDate { get; set; }
        public bool MoveDept { get; set; }
    }
}