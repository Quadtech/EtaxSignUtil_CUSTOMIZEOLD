using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace EtaxSignUtil.RC_INV
{
    public class DocumentFooter : EtaxSignUtil.Layout.DocumentFooter
    {
        [StringLength(3)]
        [Description("รหัสประเภทภาษี[TAX_TYPE_CODE]")]
        public override string TAX_TYPE_CODE { get; set; }


        [StringLength(5)]
        [Description("อัตราภาษี[TAX_CAL_RATE]")]
        public override string TAX_CAL_RATE { get; set; }


        [StringLength(16)]
        [Description("มูลค่าสินค้า/บริการ (ไม่รวมภาษีมูลค่าเพิ่ม)[BASIS_AMOUNT]")]
        public override string BASIS_AMOUNT { get; set; }


        [StringLength(3)]
        [Description("มูลค่าสินค้า/บริการ (ไม่รวมภาษีมูลค่าเพิ่ม)[BASIS_CURRENCY_CODE]")]
        public override string BASIS_CURRENCY_CODE { get; set; }


        [StringLength(16)]
        [Description("มูลค่าภาษีมูลค่าเพิ่ม[TAX_CAL_AMOUNT]")]
        public override string TAX_CAL_AMOUNT { get; set; }


        [StringLength(3)]
        [Description("มูลค่าภาษีมูลค่าเพิ่ม[TAX_CAL_CURRENCY_CODE]")]
        public override string TAX_CAL_CURRENCY_CODE { get; set; }


        [StringLength(16)]
        [Description("รวมมูลค่าตามรายการ/มูลค่าที่ถูกต้อง[LINE_TOTAL_AMOUNT]")]
        public override string LINE_TOTAL_AMOUNT { get; set; }


        [StringLength(3)]
        [Description("รวมมูลค่าตามรายการ/มูลค่าที่ถูกต้อง[LINE_TOTAL_CURRENCY_CODE]")]
        public override string LINE_TOTAL_CURRENCY_CODE { get; set; }

        [StringLength(16)]
        [Description("มูลค่าที่นำมาคิดภาษีมูลค่าเพิ่ม[TAX_BASIS_TOTAL_AMOUNT]")]
        public override string TAX_BASIS_TOTAL_AMOUNT { get; set; }


        [StringLength(3)]
        [Description("มูลค่าที่นำมาคิดภาษีมูลค่าเพิ่ม[TAX_BASIS_TOTAL_CURRENCY_CODE]")]
        public override string TAX_BASIS_TOTAL_CURRENCY_CODE { get; set; }


        [StringLength(16)]
        [Description("จำนวนภาษีมูลค่าเพิ่ม[TAX_TOTAL_AMOUNT]")]
        public override string TAX_TOTAL_AMOUNT { get; set; }


        [StringLength(3)]
        [Description("จำนวนภาษีมูลค่าเพิ่ม[TAX_TOTAL_CURRENCY_CODE]")]
        public override string TAX_TOTAL_CURRENCY_CODE { get; set; }


        [StringLength(16)]
        [Description("จำนวนเงินรวม (รวมภาษีมูลค่าเพิ่ม)[GRAND_TOTAL_AMOUNT]")]
        public override string GRAND_TOTAL_AMOUNT { get; set; }


        [StringLength(3)]
        [Description("จำนวนเงินรวม (รวมภาษีมูลค่าเพิ่ม)[GRAND_TOTAL_CURRENCY_CODE]")]
        public override string GRAND_TOTAL_CURRENCY_CODE { get; set; }

        public DocumentFooter(int LINE_TOTAL_COUNT
            , DateTime DELIVERY_OCCUR_DTM
            , string INVOICE_CURRENCY_CODE
            , bool INVOICE_INCLUDE_VAT
            , double INVOICE_TOTAL_AMOUNT
            , double DISCOUNT_AMOUNT
            , double INVOICE_TOTAL_AMOUNT_AFTER_DISCOUNT
            , double TAX_RATE
            , double TAX_AMOUNT
            , double INVOICE_TOTAL_AMOUNT_AFTER_TAX
            , double PRODUCT_DISCOUNT_AMOUNT) :
             base(LINE_TOTAL_COUNT
             , DELIVERY_OCCUR_DTM
             , INVOICE_CURRENCY_CODE
             , INVOICE_INCLUDE_VAT
             , INVOICE_TOTAL_AMOUNT
             , DISCOUNT_AMOUNT
             , INVOICE_TOTAL_AMOUNT_AFTER_DISCOUNT
             , TAX_RATE
             , TAX_AMOUNT
             , INVOICE_TOTAL_AMOUNT_AFTER_TAX
             , PRODUCT_DISCOUNT_AMOUNT
             , null)
        {

        }

        public DocumentFooter(int LINE_TOTAL_COUNT, string INVOICE_CURRENCY_CODE, double INVOICE_TOTAL_AMOUNT_AFTER_TAX)
            : base(LINE_TOTAL_COUNT, INVOICE_CURRENCY_CODE, INVOICE_TOTAL_AMOUNT_AFTER_TAX)
        {

        }
    }
}
