using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace EtaxSignUtil.RC_INV
{
    public class TradeLineItemInformation : EtaxSignUtil.Layout.TradeLineItemInformation
    {
        [StringLength(3)]
        [Description("รหัสประเภทภาษี[LINE_TAX_TYPE_CODE]")]
        public override string LINE_TAX_TYPE_CODE { get; set; }

        [StringLength(5)]
        [Description("อัตราภาษี[LINE_TAX_CAL_RATE]")]
        public override string LINE_TAX_CAL_RATE { get; set; }

        [StringLength(16)]
        [Description("มูลค่าสินค้า/บริการ (ไม่รวมภาษีมูลค่าเพิ่ม[LINE_BASIS_AMOUNT]")]
        public override string LINE_BASIS_AMOUNT { get; set; }

        [StringLength(3)]
        [Description("รหัสสกุลเงิน (มูลค่าสินค้า/บริการ)[LINE_BASIS_CURRENCY_CODE]")]
        public override string LINE_BASIS_CURRENCY_CODE { get; set; }

        [StringLength(16)]
        [Description("มูลค่าภาษีมูลค่าเพิ่ม[LINE_TAX_CAL_AMOUNT]")]
        public override string LINE_TAX_CAL_AMOUNT { get; set; }

        [StringLength(3)]
        [Description("รหัสสกุลเงิน (มูลค่าภาษีมูลค่าเพิ่ม)[LINE_TAX_CAL_CURRENCY_CODE]")]
        public override string LINE_TAX_CAL_CURRENCY_CODE { get; set; }

        [StringLength(16)]
        [Description("จำนวนเงินรวมก่อนภาษี[LINE_NET_TOTAL_AMOUNT]")]
        public override string LINE_NET_TOTAL_AMOUNT { get; set; }

        [StringLength(3)]
        [Description("รหัสสกุลเงิน (จำนวนเงินรวมก่อนภาษี)[LINE_NET_TOTAL_CURRENCY_CODE]")]
        public override string LINE_NET_TOTAL_CURRENCY_CODE { get; set; }

        [StringLength(16)]
        [Description("จำนวนเงินรวม[LINE_NET_INCLUDE_TAX_TOTAL_AMOUNT]")]
        public override string LINE_NET_INCLUDE_TAX_TOTAL_AMOUNT { get; set; }

        [StringLength(3)]
        [Description("รหัสสกุลเงิน (จำนวนเงินรวม)[LINE_NET_INCLUDE_TAX_TOTAL_CURRENCY_CODE]")]
        public override string LINE_NET_INCLUDE_TAX_TOTAL_CURRENCY_CODE { get; set; }

        public TradeLineItemInformation(int LINE_ID
            , string PRODUCT_ID
            , string PRODUCT_NAME
            , double PRODUCT_QUANTITY
            , string PRODUCT_UNIT_CODE
            , string PRODUCT_CURRENCY_CODE
            , bool PRODUCT_INCLUDE_VAT
            , double LINE_TAX_RATE
            , double PRODUCT_UNIT_PRICE
            , double PRODUCT_TOTAL_AMOUNT
            , double PRODUCT_TOTAL_AMOUNT_AFTER_DISCOUNT
            , string PRODUCT_REMARK
            , CalculateItem CAL
            , bool LAST_DETAIS)
            : base(LINE_ID
            , PRODUCT_ID
            , PRODUCT_NAME
            , PRODUCT_QUANTITY
            , PRODUCT_UNIT_CODE
            , PRODUCT_CURRENCY_CODE
            , PRODUCT_INCLUDE_VAT
            , LINE_TAX_RATE
            , PRODUCT_UNIT_PRICE
            , PRODUCT_TOTAL_AMOUNT
            , PRODUCT_TOTAL_AMOUNT_AFTER_DISCOUNT
            , PRODUCT_REMARK
            , CAL
            , LAST_DETAIS)
        {

        }
        public TradeLineItemInformation(int LINE_ID
            , string PRODUCT_NAME
            , double PRODUCT_QUANTITY
            , string PRODUCT_UNIT_CODE
            , string PRODUCT_CURRENCY_CODE
            , double PRODUCT_TOTAL_AMOUNT_AFTER_DISCOUNT)
            : base(LINE_ID
            , PRODUCT_NAME
            , PRODUCT_QUANTITY
            , PRODUCT_UNIT_CODE
            , PRODUCT_CURRENCY_CODE
            , PRODUCT_TOTAL_AMOUNT_AFTER_DISCOUNT)
        {

        }
    }
}
