using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtaxSignUtil.Layout;
using DBUtil;
using System.Data;
using ModuleUtil;
using XmlLayout;

namespace EtaxSignUtil.SI
{
    public class SILayout_Customize : SILayout
    {
        public SILayout_Customize(DBSimple dbSimple, QEB.Center QEBCenterInfo)
            : base(dbSimple, QEBCenterInfo)
        {

        }
        public override int InitLayout(DataRow rowHeader, ref string ErrMsg)
        {
            int TransErr = 0;
            int ErrCode = 0;
            bool val = this.GetSellerInfomation(ref ErrMsg);
            if (!val)
                return ++TransErr;
            string DomesticExport = ReceiveValue.StringReceive("DomesticExport", rowHeader);
            string CashCredit = ReceiveValue.StringReceive("CashCredit", rowHeader);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowHeader, 0);
            string QueryHeader =
$@"SELECT SI.InvoiceNo
,SI.InvoiceDate
,SI.PORefNo
,SI.OurPORefNo
,SI.DeliveryInstruction
,SI.DispatchDate
,SI.PaymentTermCode
,SI.PaymentDueDate
,PT.DescriptionOnSalesThai AS PaymentTermDescription
,PT.DaysToDue AS PaymentTermAddDay
,SI.CustomerCode
,LTRIM(RTRIM( ISNULL(CM.PrefixThai,'')+' '+ ISNULL(CM.NameThai,'')+' '+ISNULL(CM.SuffixThai,''))) AS CustomerName
,CM.PersonalityType
,CM.TaxID AS TaxID
,CM.HeadQuaterOrBranch AS HeadQuaterOrBranch
,CM.HeadQuaterOrBranchCode AS HeadQuaterOrBranchCode
,CM.Phone1 AS CustomerPhone
,CM.Email AS EMail
,CM.AddressThai AS AddressThai
,CM.AddressEng AS AddressEng
,T.Code AS KwaengTambonCode
,T.T AS KwaengTambon
,T.EtaxCode AS KwaengTambonEtax
,A.Code AS KhetAmporCityCode
,A.T AS KhetAmporCity
,A.EtaxCode AS KhetAmporCityEtax
,P.Code AS ProvinceStateCode
,P.T AS ProvinceState
,P.EtaxCode AS ProvinceStateEtax
,P.KwaengKhetTrueFalse AS KwaengKhetTrueFalse
,UPPER(CM.Country) AS CountryCode
,CM.PostalCode AS PostalCode
,ISNULL(CM.DomesticExport,'D') AS DomesticExport
,SI.CurrencyCode
,CAST(0 AS BIT) AS TotalAmountIncludeVATTrueFalse
,SI.TotalAmountCurrency
,SI.AmountDiscountCurrency
,SI.TotalAmountCurrencyAfterDiscountBeforeVAT
,SI.VATRate 
,SI.VATAmountCurrency
,SI.TotalAmountCurrencyAfterVAT
,SI.RecStatus
FROM SalesInvoiceHeader SI
LEFT JOIN CustomerMaster CM
ON CM.Code = SI.CustomerCode
AND CM.RecStatus = 0
LEFT JOIN QERP.dbo.ProvinceState P
ON P.CountryCode = CM.Country
AND P.Code = CM.ProvinceState
LEFT JOIN QERP.dbo.KhetAmporCity A
ON A.CountryCode = CM.Country
AND A.ProvinceStateCode = CM.ProvinceState
AND A.Code = CM.KhetAmporCity
LEFT JOIN QERP.dbo.KwaengTambon T
ON T.CountryCode = CM.Country
AND T.ProvinceStateCode = CM.ProvinceState
AND T.KhetAmporCity = CM.KhetAmporCity
AND T.Code = CM.KwaengTambon
LEFT JOIN CompanyPaymentTerm PT
ON PT.Code = SI.PaymentTermCode
WHERE SI.DomesticExport = '{DomesticExport}'
AND SI.InvoiceType = 'T'
AND SI.CashCredit = '{CashCredit}'
AND SI.RunNo = {RunNo}";
            DataTable TBHeader = new DataTable();
            TransErr = dbSimple.FillData(TBHeader, QueryHeader, ref ErrCode, ref ErrMsg);
            if (TransErr != 0 || TBHeader.Rows.Count == 0)
            {
                ErrMsg = "No Header Select";
                return ++TransErr;
            }

            string QueryDetails =
$@"SELECT TransactionType
, TransactionCode
, TransactionDescription
, Quantity
, SalesUnitCode
, UnitPriceCurrency
, TotalAmountCurrency
, TotalAmountAfterDiscount
, Note
, GDNNo
FROM SalesInvoiceDetails
WHERE DomesticExport = '{DomesticExport}'
AND InvoiceType = 'T'
AND CashCredit = '{CashCredit}'
AND RunNo = {RunNo}
AND TransactionType IN ('I','B' ,'C')
ORDER BY VLine";
            DataTable TBDetails = new DataTable();
            TransErr = dbSimple.FillData(TBDetails, QueryDetails, ref ErrCode, ref ErrMsg);
            if (TransErr != 0 || TBDetails.Rows.Count == 0)
            {
                ErrMsg = "No Details Select";
                return ++TransErr;
            }

            DataRow rowHeaderUpdate = TBHeader.Rows[0];
            string InvoiceNo = ReceiveValue.StringReceive("InvoiceNo", rowHeaderUpdate);
            DateTime InvoiceDate = ReceiveValue.DateReceive("InvoiceDate", rowHeaderUpdate, DateTime.Now.Date);
            string RefPONo = ReceiveValue.StringReceive("PORefNo", rowHeaderUpdate);
            string OurPORefNo = ReceiveValue.StringReceive("OurPORefNo", rowHeaderUpdate);
            string Remark = ReceiveValue.StringReceive("DeliveryInstruction", rowHeaderUpdate);
            DateTime DispatchDate = ReceiveValue.DateReceive("DispatchDate", rowHeaderUpdate, DateTime.Now.Date);
            string PaymentTermCode = ReceiveValue.StringReceive("PaymentTermCode", rowHeaderUpdate);
            DateTime PaymentDueDate = ReceiveValue.DateReceive("PaymentDueDate", rowHeaderUpdate, DateTime.Now);
            string PaymentTermDescription = ReceiveValue.StringReceive("PaymentTermDescription", rowHeaderUpdate);
            int PaymentTermAddDay = ReceiveValue.IntReceive("PaymentTermAddDay", rowHeaderUpdate, 0);
            int RecStatus = ReceiveValue.IntReceive("RecStatus", rowHeaderUpdate, 0);

            string CustomerCode = ReceiveValue.StringReceive("CustomerCode", rowHeaderUpdate);
            string CustomerName = ReceiveValue.StringReceive("CustomerName", rowHeaderUpdate);
            string PersonalityType = ReceiveValue.StringReceive("PersonalityType", rowHeaderUpdate);
            string TaxID = ReceiveValue.StringReceive("TaxID", rowHeaderUpdate);
            string HeadQuaterOrBranch = ReceiveValue.StringReceive("HeadQuaterOrBranch", rowHeaderUpdate);
            string HeadQuaterOrBranchCode = ReceiveValue.StringReceive("HeadQuaterOrBranchCode", rowHeaderUpdate);
            string EMail = ReceiveValue.StringReceive("EMail", rowHeaderUpdate);
            string CustomerPhone = ReceiveValue.StringReceive("CustomerPhone", rowHeaderUpdate);

            if (CustomerPhone.Length > 35)
            {
                string[] listPhone = CustomerPhone.Split(',');
                if (listPhone.Length > 0)
                    CustomerPhone = listPhone[0].Trim();
            }
            string AddressThai = ReceiveValue.StringReceive("AddressThai", rowHeaderUpdate).Trim();
            string AddressEng = ReceiveValue.StringReceive("AddressEng", rowHeaderUpdate).Trim();
            int KwaengTambonCode = ReceiveValue.IntReceive("KwaengTambonCode", rowHeaderUpdate, 0);
            string KwaengTambon = ReceiveValue.StringReceive("KwaengTambon", rowHeaderUpdate);
            int KhetAmporCityCode = ReceiveValue.IntReceive("KhetAmporCityCode", rowHeaderUpdate, 0);
            string KhetAmporCity = ReceiveValue.StringReceive("KhetAmporCity", rowHeaderUpdate);
            int ProvinceStateCode = ReceiveValue.IntReceive("ProvinceStateCode", rowHeaderUpdate, 0);
            string ProvinceState = ReceiveValue.StringReceive("ProvinceState", rowHeaderUpdate);
            bool KwaengKhetTrueFalse = ReceiveValue.BoolReceive("KwaengKhetTrueFalse", rowHeaderUpdate, false);
            string CountryCode = ReceiveValue.StringReceive("CountryCode", rowHeaderUpdate);
            string PostalCode = ReceiveValue.StringReceive("PostalCode", rowHeaderUpdate).Trim();
            string KwaengTambonEtax = ReceiveValue.StringReceive("KwaengTambonEtax", rowHeaderUpdate);
            string KhetAmporCityEtax = ReceiveValue.StringReceive("KhetAmporCityEtax", rowHeaderUpdate);
            string ProvinceStateEtax = ReceiveValue.StringReceive("ProvinceStateEtax", rowHeaderUpdate);
            bool CustomerDomesticExport = (ReceiveValue.StringReceive("DomesticExport", rowHeaderUpdate) == "D" ? true : false);

            string CurrencyCode = ReceiveValue.StringReceive("CurrencyCode", rowHeaderUpdate);
            bool TotalAmountIncludeVATTrueFalse = ReceiveValue.BoolReceive("TotalAmountIncludeVATTrueFalse", rowHeaderUpdate, false);
            double TotalAmountCurrency = ReceiveValue.DoubleReceive("TotalAmountCurrency", rowHeaderUpdate, 0);
            double AmountDiscountCurrency = ReceiveValue.DoubleReceive("AmountDiscountCurrency", rowHeaderUpdate, 0);
            double TotalAmountCurrencyAfterDiscountBeforeVAT = ReceiveValue.DoubleReceive("TotalAmountCurrencyAfterDiscountBeforeVAT", rowHeaderUpdate, 0);
            double VATRate = ReceiveValue.DoubleReceive("VATRate", rowHeaderUpdate, 7);
            double VATAmountCurrency = ReceiveValue.DoubleReceive("VATAmountCurrency", rowHeaderUpdate, 0);
            double TotalAmountCurrencyAfterVAT = ReceiveValue.DoubleReceive("TotalAmountCurrencyAfterVAT", rowHeaderUpdate, 0);

            this.FileControl = new FileControl(this.SellerInformation.SELLER_TAX_ID, this.SellerInformation.SELLER_BRANCH_ID);

            this.DocumentHeader = new SI.DocumentHeader(InvoiceNo
             , InvoiceDate
             , RefPONo
             , OurPORefNo
             , Remark);


            if (String.IsNullOrEmpty(this.DocumentHeader.BUYER_ORDER_ASSIGN_ID))
            {
                ErrMsg = "ไม่พบเลขที่ใบสั่งซื้อ";
                return ++TransErr;
            }

            this.BuyerInformation = new BuyerInformation(CustomerCode
              , CustomerName
              , PersonalityType
              , TaxID
              , HeadQuaterOrBranch
              , HeadQuaterOrBranchCode
              , EMail
              , CustomerPhone
              , AddressThai
              , KwaengTambonCode
              , KwaengTambon
              , KhetAmporCityCode
              , KhetAmporCity
              , ProvinceStateCode
              , ProvinceState
              , KwaengKhetTrueFalse
              , CountryCode
              , PostalCode
              , KwaengTambonEtax
              , KhetAmporCityEtax
              , ProvinceStateEtax
              , CustomerDomesticExport);

            TransErr += this.BuyerInformation.ValidateInfo(ref ErrMsg);
            if (TransErr != 0)
                return TransErr;

            int index = 0;
            double TotalDisCount = 0;
            var cal = new CalculateItem(AmountDiscountCurrency, TotalAmountCurrencyAfterDiscountBeforeVAT, TotalAmountCurrencyAfterVAT);
            this.ListTradeLineItemInformation = new List<TradeLineItemInformation>();
            foreach (DataRow rowSIDetailsUpdate in TBDetails.Rows)
            {
                index++;
                string TransactionType = ReceiveValue.StringReceive("TransactionType", rowSIDetailsUpdate);
                string TransactionCode = ReceiveValue.StringReceive("TransactionCode", rowSIDetailsUpdate);
                string TransactionDescription = ReceiveValue.StringReceive("TransactionDescription", rowSIDetailsUpdate);
                double Quantity = ReceiveValue.DoubleReceive("Quantity", rowSIDetailsUpdate, 0);
                string SalesUnitCode = ReceiveValue.StringReceive("SalesUnitCode", rowSIDetailsUpdate);
                double UnitPriceCurrency = ReceiveValue.DoubleReceive("UnitPriceCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountCurrencyDetails = ReceiveValue.DoubleReceive("TotalAmountCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountAfterDiscount = ReceiveValue.DoubleReceive("TotalAmountAfterDiscount", rowSIDetailsUpdate, 0);
                string Note = ReceiveValue.StringReceive("Note", rowSIDetailsUpdate);
                string GDNNo = ReceiveValue.StringReceive("GDNNo", rowSIDetailsUpdate);
                TradeLineItemInformation TradeLineItemInformation = new TradeLineItemInformation(index
                    , TransactionType
                    , TransactionCode
                    , TransactionDescription
                    , Quantity
                    , SalesUnitCode
                    , GDNNo
                    , CurrencyCode
                    , TotalAmountIncludeVATTrueFalse
                    , VATRate
                    , UnitPriceCurrency
                    , TotalAmountCurrencyDetails
                    , TotalAmountAfterDiscount
                    , Note
                    , cal
                    , index == TBDetails.Rows.Count ? true : false);

                TotalDisCount += Convert.ToDouble(TradeLineItemInformation.PRODUCT_ALLOWANCE_ACTUAL_AMOUNT);
                TotalDisCount = Math.Round(TotalDisCount, 2);

                this.ListTradeLineItemInformation.Add(TradeLineItemInformation);
            }
            this.DocumentFooter = new DocumentFooter(index
                , DispatchDate
                , CurrencyCode
                , TotalAmountIncludeVATTrueFalse
                , TotalAmountCurrency
                , AmountDiscountCurrency
                , TotalAmountCurrencyAfterDiscountBeforeVAT
                , VATRate
                , VATAmountCurrency
                , TotalAmountCurrencyAfterVAT
                , PaymentTermCode
                , PaymentTermDescription
                , PaymentDueDate
                , PaymentTermAddDay
                , TotalDisCount);

            this.FileTrailer = new FileTrailer(1);
            return TransErr;
        }
        public TaxInvoice_CrossIndustryInvoice GetCrossIndustryInvoice()
        {
            TaxInvoice_CrossIndustryInvoice invoice = new TaxInvoice_CrossIndustryInvoice();
            #region ExchangedDocument
            ExchangedDocument ExchangedDocumentObj = invoice.ExchangedDocument;
            ExchangedDocumentObj.ID = DocumentHeader.DOCUMENT_ID;
            ExchangedDocumentObj.Name = DocumentHeader.DOCUMENT_NAME;
            ExchangedDocumentObj.TypeCode.value = DocumentHeader.DOCUMENT_TYPE_CODE;
            ExchangedDocumentObj.IssueDateTime = DocumentHeader.DOCUMENT_ISSUE_DTM;
            ExchangedDocumentObj.CreationDateTime = DocumentHeader.ConvertISODateTime(DateTime.Now);
            List<IncludedNote> ListNote = new List<IncludedNote>();
            if (!String.IsNullOrEmpty(DocumentHeader.DOCUMENT_REMARK))
            {
                IncludedNote note = new IncludedNote();
                note.Subject = "DocumentRemark";
                note.Content = DocumentHeader.DOCUMENT_REMARK;
                ListNote.Add(note);
            }
            if (!String.IsNullOrEmpty(DocumentFooter.PAYMENT_DUE_DTM))
            {
                IncludedNote note = new IncludedNote();
                note.Subject = "DueDateTime";
                note.Content = DocumentFooter.PAYMENT_DUE_DTM;
                ListNote.Add(note);
            }
            ExchangedDocumentObj.IncludedNote = ListNote.ToArray();
            #endregion
            #region SupplyChainTradeTransaction
            #region ApplicableHeaderTradeAgreement
            ApplicableHeaderTradeAgreement tradeAgreement = invoice.SupplyChainTradeTransaction.ApplicableHeaderTradeAgreement;
            #region SellerTradeParty
            SellerTradeParty SellerTradePartyObj = tradeAgreement.SellerTradeParty;
            SellerTradePartyObj.Name = this.SellerInformation.SELLER_NAME;
            SellerTradePartyObj.SpecifiedTaxRegistration.ID.value = this.SellerInformation.SELLER_TAX_ID + this.SellerInformation.SELLER_BRANCH_ID;
            #region Seller_PostalTradeAddress
            if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_ADDRESS_LINE1))
            {
                PostalTradeAddress PostalTradeAddress = SellerTradePartyObj.PostalTradeAddress;
                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_POST_CODE))
                    PostalTradeAddress.PostcodeCode = this.SellerInformation.SELLER_POST_CODE;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_ADDRESS_LINE1))
                    PostalTradeAddress.LineOne = this.SellerInformation.SELLER_ADDRESS_LINE1;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_ADDRESS_LINE2))
                    PostalTradeAddress.LineTwo = this.SellerInformation.SELLER_ADDRESS_LINE2;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_COUNTRY_ID))
                    PostalTradeAddress.CountryID = this.SellerInformation.SELLER_COUNTRY_ID;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID))
                    PostalTradeAddress.CountrySubDivisionID = this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_BUILDING_NO))
                    PostalTradeAddress.BuildingNumber = this.SellerInformation.SELLER_BUILDING_NO;
            }
            else
            {
                PostalTradeAddress PostalTradeAddress = SellerTradePartyObj.PostalTradeAddress;
                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_POST_CODE))
                    PostalTradeAddress.PostcodeCode = this.SellerInformation.SELLER_POST_CODE;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_BUILDING_NAME))
                    PostalTradeAddress.BuildingName = this.SellerInformation.SELLER_BUILDING_NAME;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_ADDRESS_LINE3))
                    PostalTradeAddress.LineThree = this.SellerInformation.SELLER_ADDRESS_LINE3;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_ADDRESS_LINE4))
                    PostalTradeAddress.LineFour = this.SellerInformation.SELLER_ADDRESS_LINE4;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_ADDRESS_LINE5))
                    PostalTradeAddress.LineFive = this.SellerInformation.SELLER_ADDRESS_LINE5;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_STREET_NAME))
                    PostalTradeAddress.StreetName = this.SellerInformation.SELLER_STREET_NAME;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_CITY_ID))
                    PostalTradeAddress.CityName = this.SellerInformation.SELLER_CITY_ID;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_CITY_SUB_DIV_ID))
                    PostalTradeAddress.CitySubDivisionName = this.SellerInformation.SELLER_CITY_SUB_DIV_ID;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_COUNTRY_ID))
                    PostalTradeAddress.CountryID = this.SellerInformation.SELLER_COUNTRY_ID;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID))
                    PostalTradeAddress.CountrySubDivisionID = this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID;

                if (!String.IsNullOrEmpty(this.SellerInformation.SELLER_BUILDING_NO))
                    PostalTradeAddress.BuildingNumber = this.SellerInformation.SELLER_BUILDING_NO;
            }
            #endregion
            #endregion

            #region BuyerTradeParty
            BuyerTradeParty BuyerTradePartyObj = tradeAgreement.BuyerTradeParty;
            BuyerTradePartyObj.ID = this.BuyerInformation.BUYER_ID;
            BuyerTradePartyObj.Name = this.BuyerInformation.BUYER_NAME;
            BuyerTradePartyObj.SpecifiedTaxRegistration.ID.schemeID = this.BuyerInformation.BUYER_TAX_ID_TYPE;
            BuyerTradePartyObj.SpecifiedTaxRegistration.ID.value = this.BuyerInformation.BUYER_TAX_ID + this.BuyerInformation.BUYER_BRANCH_ID;
            #region DefinedTradeContact
            // ตัดช่องว่างหัวท้ายก่อนเช็ค กันอีเมลลูกค้าที่มีแต่ช่องว่างทำให้ tag URIID ว่าง (schema กรมสรรพากรบังคับอย่างน้อย 1 ตัวอักษร)
            string BuyerURIID = (BuyerInformation.BUYER_URIID ?? "").Trim();
            if (BuyerURIID != "")
            {
                BuyerTradePartyObj.DefinedTradeContact.EmailURIUniversalCommunication.URIID = BuyerURIID;
            }
            #endregion
            #region Buyer_PostalTradeAddress
            if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_ADDRESS_LINE1))
            {
                PostalTradeAddress PostalTradeAddress = BuyerTradePartyObj.PostalTradeAddress;
                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_POST_CODE))
                    PostalTradeAddress.PostcodeCode = this.BuyerInformation.BUYER_POST_CODE;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_ADDRESS_LINE1))
                    PostalTradeAddress.LineOne = this.BuyerInformation.BUYER_ADDRESS_LINE1;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_ADDRESS_LINE2))
                    PostalTradeAddress.LineTwo = this.BuyerInformation.BUYER_ADDRESS_LINE2;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_COUNTRY_ID))
                    PostalTradeAddress.CountryID = this.BuyerInformation.BUYER_COUNTRY_ID;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID))
                    PostalTradeAddress.CountrySubDivisionID = this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_BUILDING_NO))
                    PostalTradeAddress.BuildingNumber = this.BuyerInformation.BUYER_BUILDING_NO;
            }
            else
            {
                PostalTradeAddress PostalTradeAddress = BuyerTradePartyObj.PostalTradeAddress;
                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_POST_CODE))
                    PostalTradeAddress.PostcodeCode = this.BuyerInformation.BUYER_POST_CODE;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_BUILDING_NAME))
                    PostalTradeAddress.BuildingName = this.BuyerInformation.BUYER_BUILDING_NAME;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_ADDRESS_LINE3))
                    PostalTradeAddress.LineThree = this.BuyerInformation.BUYER_ADDRESS_LINE3;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_ADDRESS_LINE4))
                    PostalTradeAddress.LineFour = this.BuyerInformation.BUYER_ADDRESS_LINE4;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_ADDRESS_LINE5))
                    PostalTradeAddress.LineFive = this.BuyerInformation.BUYER_ADDRESS_LINE5;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_STREET_NAME))
                    PostalTradeAddress.StreetName = this.BuyerInformation.BUYER_STREET_NAME;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_CITY_ID))
                    PostalTradeAddress.CityName = this.BuyerInformation.BUYER_CITY_ID;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_CITY_SUB_DIV_ID))
                    PostalTradeAddress.CitySubDivisionName = this.BuyerInformation.BUYER_CITY_SUB_DIV_ID;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_COUNTRY_ID))
                    PostalTradeAddress.CountryID = this.BuyerInformation.BUYER_COUNTRY_ID;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID))
                    PostalTradeAddress.CountrySubDivisionID = this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID;

                if (!String.IsNullOrEmpty(this.BuyerInformation.BUYER_BUILDING_NO))
                    PostalTradeAddress.BuildingNumber = this.BuyerInformation.BUYER_BUILDING_NO;
            }
            #endregion
            #endregion

            #region BuyerOrderReferencedDocument
            BuyerOrderReferencedDocument refDocument = tradeAgreement.BuyerOrderReferencedDocument;
            if (!String.IsNullOrEmpty(DocumentHeader.BUYER_ORDER_ASSIGN_ID))
                refDocument.IssuerAssignedID = DocumentHeader.BUYER_ORDER_ASSIGN_ID;

            if (!String.IsNullOrEmpty(DocumentHeader.BUYER_ORDER_ASSIGN_ID2))
                refDocument.IssuerAssignedID2 = DocumentHeader.BUYER_ORDER_ASSIGN_ID2;
            #endregion
            #endregion

            #region ApplicableHeaderTradeDelivery
            ApplicableHeaderTradeDelivery tradeDelivery = invoice.SupplyChainTradeTransaction.ApplicableHeaderTradeDelivery;
            if (!String.IsNullOrEmpty(DocumentFooter.DELIVERY_OCCUR_DTM))
                tradeDelivery.ActualDeliverySupplyChainEvent.OccurrenceDateTime = DocumentFooter.DELIVERY_OCCUR_DTM;
            #endregion

            #region ApplicableHeaderTradeSettlement
            ApplicableHeaderTradeSettlement tradeSettlement = invoice.SupplyChainTradeTransaction.ApplicableHeaderTradeSettlement;
            #region InvoiceCurrencyCode
            tradeSettlement.InvoiceCurrencyCode = DocumentFooter.INVOICE_CURRENCY_CODE;
            #endregion

            #region ApplicableTradeTax
            ApplicableTradeTax ApplicableTradeTax = tradeSettlement.ApplicableTradeTax;
            ApplicableTradeTax.TypeCode = DocumentFooter.TAX_TYPE_CODE;
            ApplicableTradeTax.CalculatedRate = DocumentFooter.TAX_CAL_RATE;
            ApplicableTradeTax.BasisAmount.currencyID = DocumentFooter.BASIS_CURRENCY_CODE;
            ApplicableTradeTax.BasisAmount.value = DocumentFooter.BASIS_AMOUNT;
            ApplicableTradeTax.CalculatedAmount.currencyID = DocumentFooter.TAX_CAL_CURRENCY_CODE;
            ApplicableTradeTax.CalculatedAmount.value = DocumentFooter.TAX_CAL_AMOUNT;
            #endregion

            #region SpecifiedTradeAllowanceCharge
            SpecifiedTradeAllowanceCharge tradeAllowanceCharge = tradeSettlement.SpecifiedTradeAllowanceCharge;
            tradeAllowanceCharge.ChargeIndicator = DocumentFooter.ALLOWANCE_CHARGE_IND;
            tradeAllowanceCharge.ActualAmount.currencyID = DocumentFooter.ALLOWANCE_TOTAL_CURRENCY_CODE;
            tradeAllowanceCharge.ActualAmount.value = DocumentFooter.ALLOWANCE_ACTUAL_AMOUNT;
            tradeAllowanceCharge.TypeCode = "95";
            #endregion

            #region SpecifiedTradePaymentTerms
            SpecifiedTradePaymentTerms tradePaymentTerms = tradeSettlement.SpecifiedTradePaymentTerms;
            tradePaymentTerms.Description = DocumentFooter.PAYMENT_DESCRIPTION;
            tradePaymentTerms.DueDateDateTime = DocumentFooter.PAYMENT_DUE_DTM;
            tradePaymentTerms.TypeCode = DocumentFooter.PAYMENT_TYPE_CODE;
            #endregion

            #region SpecifiedTradeSettlementHeaderMonetarySummation
            SpecifiedTradeSettlementHeaderMonetarySummation summation = tradeSettlement.SpecifiedTradeSettlementHeaderMonetarySummation;
            summation.LineTotalAmount.currencyID = DocumentFooter.LINE_TOTAL_CURRENCY_CODE;
            summation.LineTotalAmount.value = DocumentFooter.LINE_TOTAL_AMOUNT;
            summation.AllowanceTotalAmount.currencyID = DocumentFooter.ALLOWANCE_TOTAL_CURRENCY_CODE;
            summation.AllowanceTotalAmount.value = DocumentFooter.ALLOWANCE_TOTAL_AMOUNT;
            summation.TaxBasisTotalAmount.currencyID = DocumentFooter.TAX_BASIS_TOTAL_CURRENCY_CODE;
            summation.TaxBasisTotalAmount.value = DocumentFooter.TAX_BASIS_TOTAL_AMOUNT;
            summation.TaxTotalAmount.currencyID = DocumentFooter.TAX_TOTAL_CURRENCY_CODE;
            summation.TaxTotalAmount.value = DocumentFooter.TAX_TOTAL_AMOUNT;
            summation.GrandTotalAmount.currencyID = DocumentFooter.GRAND_TOTAL_CURRENCY_CODE;
            summation.GrandTotalAmount.value = DocumentFooter.GRAND_TOTAL_AMOUNT;
            #endregion
            #endregion

            List<IncludedSupplyChainTradeLineItem> listDetails = new List<IncludedSupplyChainTradeLineItem>();
            foreach (TradeLineItemInformation item in this.ListTradeLineItemInformation)
            {
                IncludedSupplyChainTradeLineItem lineItem = new IncludedSupplyChainTradeLineItem();
                #region IncludedSupplyChainTradeLineItem
                #region AssociatedDocumentLineDocument
                AssociatedDocumentLineDocument lineDocument = lineItem.AssociatedDocumentLineDocument;
                lineDocument.LineID = item.LINE_ID;
                #endregion

                #region SpecifiedTradeProduct
                SpecifiedTradeProduct tradeProduct = lineItem.SpecifiedTradeProduct;
                tradeProduct.ID.transactionType = item.PRODUCT_TRANSACTIONTYPE;
                tradeProduct.ID.value = item.PRODUCT_ID;
                tradeProduct.Name = item.PRODUCT_NAME;
                List<IncludedNote> ListNoteItem = new List<IncludedNote>();
                #region InformationNote
                if (!String.IsNullOrEmpty(item.PRODUCT_REMARK))
                {
                    IncludedNote note = new IncludedNote();
                    note.Subject = "ProductRemark";
                    note.Content = item.PRODUCT_REMARK;
                    ListNoteItem.Add(note);
                }
                #endregion
                tradeProduct.InformationNote = ListNoteItem.ToArray();
                #endregion

                #region SpecifiedLineTradeAgreement
                GrossPriceProductTradePrice productTradePrice = lineItem.SpecifiedLineTradeAgreement.GrossPriceProductTradePrice;
                productTradePrice.ChargeAmount.currencyID = item.PRODUCT_CHARGE_CURRENCY_CODE;
                productTradePrice.ChargeAmount.value = item.PRODUCT_CHARGE_AMOUNT;
                #endregion

                #region SpecifiedLineTradeDelivery
                BilledQuantity billedQuantity = lineItem.SpecifiedLineTradeDelivery.BilledQuantity;
                billedQuantity.unitCode = item.PRODUCT_UNIT_CODE;
                billedQuantity.value = item.PRODUCT_QUANTITY;
                DeliveryOrderReferencedDocument deliveryOrder = lineItem.SpecifiedLineTradeDelivery.DeliveryOrderReferencedDocument;
                deliveryOrder.IssuerAssignedID = item.ADDITIONAL_REF_ASSIGN_ID;
                #endregion

                #region SpecifiedLineTradeSettlement
                SpecifiedLineTradeSettlement lineTradeSettlement = lineItem.SpecifiedLineTradeSettlement;
                #region ApplicableTradeTax
                ApplicableTradeTax applicableTradeTax = lineTradeSettlement.ApplicableTradeTax;
                applicableTradeTax.TypeCode = item.LINE_TAX_TYPE_CODE;
                applicableTradeTax.CalculatedRate = item.LINE_TAX_CAL_RATE;
                applicableTradeTax.BasisAmount.currencyID = item.LINE_BASIS_CURRENCY_CODE;
                applicableTradeTax.BasisAmount.value = item.LINE_BASIS_AMOUNT;
                applicableTradeTax.CalculatedAmount.currencyID = item.LINE_TAX_CAL_CURRENCY_CODE;
                applicableTradeTax.CalculatedAmount.value = item.LINE_TAX_CAL_AMOUNT;
                #endregion

                #region SpecifiedTradeAllowanceCharge
                SpecifiedTradeAllowanceCharge lineTradeAllowanceCharge = lineTradeSettlement.SpecifiedTradeAllowanceCharge;
                lineTradeAllowanceCharge.ChargeIndicator = item.LINE_ALLOWANCE_CHARGE_IND;
                lineTradeAllowanceCharge.ActualAmount.currencyID = item.LINE_ALLOWANCE_ACTUAL_CURRENCY_CODE;
                lineTradeAllowanceCharge.ActualAmount.value = item.LINE_ALLOWANCE_ACTUAL_AMOUNT;
                #endregion

                #region SpecifiedTradeSettlementLineMonetarySummation
                SpecifiedTradeSettlementLineMonetarySummation lineSummation = lineTradeSettlement.SpecifiedTradeSettlementLineMonetarySummation;
                lineSummation.TaxTotalAmount.currencyID = item.LINE_BASIS_CURRENCY_CODE;
                lineSummation.TaxTotalAmount.value = item.LINE_TAX_TOTAL_AMOUNT;
                lineSummation.NetLineTotalAmount.currencyID = item.LINE_NET_TOTAL_CURRENCY_CODE;
                lineSummation.NetLineTotalAmount.value = item.LINE_NET_TOTAL_AMOUNT;
                lineSummation.NetIncludingTaxesLineTotalAmount.currencyID = item.LINE_NET_INCLUDE_TAX_TOTAL_CURRENCY_CODE;
                lineSummation.NetIncludingTaxesLineTotalAmount.value = item.LINE_NET_INCLUDE_TAX_TOTAL_AMOUNT;
                #endregion
                #endregion
                #endregion
                listDetails.Add(lineItem);
            }

            invoice.SupplyChainTradeTransaction.IncludedSupplyChainTradeLineItem = listDetails.ToArray();
            #endregion
            return invoice;
        }
    }
}
