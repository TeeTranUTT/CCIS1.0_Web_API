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
}