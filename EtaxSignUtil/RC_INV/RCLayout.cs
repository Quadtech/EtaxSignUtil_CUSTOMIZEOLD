using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtaxSignUtil.Layout;
using DBUtil;
using System.Data;
using ModuleUtil;

namespace EtaxSignUtil.RC_INV
{
    public class RCLayout : BaseLayout
    {
        public FileControl FileControl { get; private set; }
        public RC_INV.DocumentHeader DocumentHeader { get; private set; }
        public BuyerInformation BuyerInformation { get; private set; }
        public List<RC_INV.TradeLineItemInformation> ListTradeLineItemInformation { get; private set; }
        public RC_INV.DocumentFooter DocumentFooter { get; private set; }
        public FileTrailer FileTrailer { get; private set; }
        public RCLayout(DBSimple dbSimple, QEB.Center QEBCenterInfo)
            : base(dbSimple, QEBCenterInfo)
        {

        }
        public int InitLayout(DataRow rowHeader, ref string ErrMsg)
        {
            int TransErr = 0;
            int ErrCode = 0;
            bool val = this.GetSellerInfomation(ref ErrMsg);
            if (!val)
                return ++TransErr;
            string ReceiptType = ReceiveValue.StringReceive("ReceiptType", rowHeader);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowHeader, 0);
            string QueryHeader =
@"SELECT H.ReceiptNo
,H.ReceiptDate
,SI.InvoiceNo
,'' AS PORefNo
,H.FieldNote
,SI.InvoiceDate AS DispatchDate
,SI.CustomerCode
,LTRIM(RTRIM( ISNULL(CM.PrefixThai,'')+' '+ ISNULL(CM.NameThai,'')+' '+ISNULL(CM.SuffixThai,''))) AS CustomerName
,CM.TaxID AS TaxID
,CM.PersonalityType
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
,CAST(0 as bit) AS TotalAmountIncludeVATTrueFalse
,SI.TotalAmountCurrency
,CAST(0 as float) AS AmountDiscountCurrency
,SI.TotalAmountCurrency AS TotalAmountCurrencyAfterDiscountBeforeVAT
,SI.VATRate 
,SI.VATAmountCurrency
,SI.TotalAmountCurrencyAfterVAT
FROM ReceiptHeader H
CROSS APPLY 
(
	SELECT TOP 1 * FROM ReceiptDetails D
	WHERE D.RecStatus = 0
	AND D.ReceiptType = H.ReceiptType
	AND D.RunNo = H.RunNo
	AND RefDocType1 ='INV'
) D
LEFT JOIN InvoiceHeader SI
ON SI.RecStatus = 0
AND SI.InvoiceNo = D.RefDocNo
LEFT JOIN CustomerMaster CM
ON CM.Code = H.ReceivableCode
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
WHERE H.ReceiptType = '{0}'
AND H.RunNo = {1}";
            QueryHeader = String.Format(QueryHeader, ReceiptType, RunNo);
            DataTable TBHeader = new DataTable();
            TransErr = dbSimple.FillData(TBHeader, QueryHeader, ref ErrCode, ref ErrMsg);
            if (TransErr != 0 || TBHeader.Rows.Count == 0)
            {
                ErrMsg = "No Header Select";
                return ++TransErr;
            }

            string QueryDetails =
@"SELECT SI.TransactionCode
, SI.TransactionDescription
, SI.Quantity
, '' AS SalesUnitCode
, SI.UnitPriceCurrency
, SI.TotalAmountCurrency
, SI.TotalAmountAfterDiscount
,'' AS Note
FROM ReceiptHeader H
CROSS APPLY 
(
	SELECT TOP 1 * FROM ReceiptDetails D
	WHERE D.RecStatus = 0
	AND D.ReceiptType = H.ReceiptType
	AND D.RunNo = H.RunNo
    AND RefDocType1 ='INV'
) D
LEFT JOIN InvoiceDetails SI
ON SI.RecStatus = 0
AND SI.InvoiceNo = D.RefDocNo
WHERE H.ReceiptType = '{0}'
AND H.RunNo = '{1}'
AND SI.TransactionType IN ('I','B')
ORDER BY D.VLine";
            QueryDetails = String.Format(QueryDetails, ReceiptType, RunNo);
            DataTable TBDetails = new DataTable();
            TransErr = dbSimple.FillData(TBDetails, QueryDetails, ref ErrCode, ref ErrMsg);
            if (TransErr != 0 || TBDetails.Rows.Count == 0)
            {
                ErrMsg = "No Details Select";
                return ++TransErr;
            }

            DataRow rowHeaderUpdate = TBHeader.Rows[0];
            string ReceiptNo = ReceiveValue.StringReceive("ReceiptNo", rowHeaderUpdate);
            DateTime ReceiptDate = ReceiveValue.DateReceive("ReceiptDate", rowHeaderUpdate, DateTime.Now.Date);
            string PORefNo = ReceiveValue.StringReceive("PORefNo", rowHeaderUpdate);
            string FieldNote = ReceiveValue.StringReceive("FieldNote", rowHeaderUpdate);
            DateTime DispatchDate = ReceiveValue.DateReceive("DispatchDate", rowHeaderUpdate, DateTime.Now.Date);

            string CustomerCode = ReceiveValue.StringReceive("CustomerCode", rowHeaderUpdate);
            string CustomerName = ReceiveValue.StringReceive("CustomerName", rowHeaderUpdate);
            string PersonalityType = ReceiveValue.StringReceive("PersonalityType", rowHeaderUpdate);
            string TaxID = ReceiveValue.StringReceive("TaxID", rowHeaderUpdate);
            string HeadQuaterOrBranch = ReceiveValue.StringReceive("HeadQuaterOrBranch", rowHeaderUpdate);
            string HeadQuaterOrBranchCode = ReceiveValue.StringReceive("HeadQuaterOrBranchCode", rowHeaderUpdate);
            string EMail = ReceiveValue.StringReceive("EMail", rowHeaderUpdate);
            string CustomerPhone = ReceiveValue.StringReceive("CustomerPhone", rowHeaderUpdate);
            string AddressThai = ReceiveValue.StringReceive("AddressThai", rowHeaderUpdate);
            string AddressEng = ReceiveValue.StringReceive("AddressEng", rowHeaderUpdate);
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
            bool DomesticExport = (ReceiveValue.StringReceive("DomesticExport", rowHeaderUpdate) == "D" ? true : false);

            string CurrencyCode = ReceiveValue.StringReceive("CurrencyCode", rowHeaderUpdate);
            bool TotalAmountIncludeVATTrueFalse = ReceiveValue.BoolReceive("TotalAmountIncludeVATTrueFalse", rowHeaderUpdate, false);
            double TotalAmountCurrency = ReceiveValue.DoubleReceive("TotalAmountCurrency", rowHeaderUpdate, 0);
            double AmountDiscountCurrency = ReceiveValue.DoubleReceive("AmountDiscountCurrency", rowHeaderUpdate, 0);
            double TotalAmountCurrencyAfterDiscountBeforeVAT = ReceiveValue.DoubleReceive("TotalAmountCurrencyAfterDiscountBeforeVAT", rowHeaderUpdate, 0);
            double VATRate = ReceiveValue.DoubleReceive("VATRate", rowHeaderUpdate, 7);
            double VATAmountCurrency = ReceiveValue.DoubleReceive("VATAmountCurrency", rowHeaderUpdate, 0);
            double TotalAmountCurrencyAfterVAT = ReceiveValue.DoubleReceive("TotalAmountCurrencyAfterVAT", rowHeaderUpdate, 0);

            this.FileControl = new FileControl(this.SellerInformation.SELLER_TAX_ID, this.SellerInformation.SELLER_BRANCH_ID);

            this.DocumentHeader = new RC_INV.DocumentHeader(ReceiptNo
             , ReceiptDate
             , PORefNo
             , FieldNote);

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
              , DomesticExport);

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
                string TransactionCode = ReceiveValue.StringReceive("TransactionCode", rowSIDetailsUpdate);
                string TransactionDescription = ReceiveValue.StringReceive("TransactionDescription", rowSIDetailsUpdate);
                double Quantity = ReceiveValue.DoubleReceive("Quantity", rowSIDetailsUpdate, 0);
                string SalesUnitCode = ReceiveValue.StringReceive("SalesUnitCode", rowSIDetailsUpdate);
                double UnitPriceCurrency = ReceiveValue.DoubleReceive("UnitPriceCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountCurrencyDetails = ReceiveValue.DoubleReceive("TotalAmountCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountAfterDiscount = ReceiveValue.DoubleReceive("TotalAmountAfterDiscount", rowSIDetailsUpdate, 0);
                string Note = ReceiveValue.StringReceive("Note", rowSIDetailsUpdate);
                RC_INV.TradeLineItemInformation TradeLineItemInformation = new RC_INV.TradeLineItemInformation(index
                    , TransactionCode
                    , TransactionDescription
                    , Quantity
                    , SalesUnitCode
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
            this.DocumentFooter = new RC_INV.DocumentFooter(index
                , DispatchDate
                , CurrencyCode
                , TotalAmountIncludeVATTrueFalse
                , TotalAmountCurrency
                , AmountDiscountCurrency
                , TotalAmountCurrencyAfterDiscountBeforeVAT
                , VATRate
                , VATAmountCurrency
                , TotalAmountCurrencyAfterVAT
                , TotalDisCount);

            this.FileTrailer = new FileTrailer(1);
            return TransErr;
        }
        public int ValidateLayout(ref string ErrMsg)
        {
            int TransErr = 0;
            if (!this.SellerInformation.Validate())
            {
                ErrMsg = this.SellerInformation.GetErrorMessage();
                return ++TransErr; ;
            }
            if (!this.FileControl.Validate())
            {
                ErrMsg = this.FileControl.GetErrorMessage();
                return ++TransErr; ;
            }
            if (!this.DocumentHeader.Validate())
            {
                ErrMsg = this.DocumentHeader.GetErrorMessage();
                return ++TransErr; ;
            }
            if (!this.BuyerInformation.Validate())
            {
                ErrMsg = this.BuyerInformation.GetErrorMessage();
                return ++TransErr; ;
            }
            foreach (TradeLineItemInformation TradeLineItemInformation in this.ListTradeLineItemInformation)
            {
                if (!TradeLineItemInformation.Validate())
                {
                    ErrMsg = TradeLineItemInformation.GetErrorMessage();
                    return ++TransErr; ;
                }
            }
            if (!this.DocumentFooter.Validate())
            {
                ErrMsg = this.DocumentFooter.GetErrorMessage();
                return ++TransErr; ;
            }
            if (!this.FileTrailer.Validate())
            {
                ErrMsg = this.FileTrailer.GetErrorMessage();
                return ++TransErr; ;
            }

            return TransErr;
        }
        private string GetTradeLineItemInformation(TradeLineItemInformation item)
        {
            string result = "";
            #region IncludedSupplyChainTradeLineItem
            #region IncludedSupplyChainTradeLineItem
            string AssociatedDocumentLineDocument =
@"{0}{0}{0}<ram:AssociatedDocumentLineDocument>
{0}{0}{0}{0}<ram:LineID>{1}</ram:LineID>
{0}{0}{0}</ram:AssociatedDocumentLineDocument>";
            AssociatedDocumentLineDocument = String.Format(AssociatedDocumentLineDocument
                , "\t"
                , item.LINE_ID);
            #endregion

            #region SpecifiedTradeProduct
            #region InformationNote
            string SpecifiedTradeProduct_InformationNote = "";
            if (item.PRODUCT_REMARK != string.Empty)
            {
                SpecifiedTradeProduct_InformationNote =
@"{0}{0}{0}{0}<ram:InformationNote>
{0}{0}{0}{0}{0}<ram:Subject>ProductRemark</ram:Subject>
{0}{0}{0}{0}{0}<ram:Content>{1}</ram:Content>
{0}{0}{0}{0}</ram:InformationNote>";
                SpecifiedTradeProduct_InformationNote = String.Format(SpecifiedTradeProduct_InformationNote
                    , item.PRODUCT_REMARK);
            }
            #endregion
            string SpecifiedTradeProduct =
@"{0}{0}{0}<ram:SpecifiedTradeProduct>
{0}{0}{0}{0}<ram:ID>{1}</ram:ID>
{0}{0}{0}{0}<ram:Name>{2}</ram:Name>
{3}
{0}{0}{0}</ram:SpecifiedTradeProduct>";
            SpecifiedTradeProduct = String.Format(SpecifiedTradeProduct
                , "\t"
                , item.PRODUCT_ID
                , item.PRODUCT_NAME
                , SpecifiedTradeProduct_InformationNote);
            #endregion

            #region SpecifiedLineTradeAgreement
            string SpecifiedLineTradeAgreement =
@"{0}{0}{0}<ram:SpecifiedLineTradeAgreement>
{0}{0}{0}{0}<ram:GrossPriceProductTradePrice>
{0}{0}{0}{0}{0}<ram:ChargeAmount currencyID=""{1}"">{2}</ram:ChargeAmount>
{0}{0}{0}{0}</ram:GrossPriceProductTradePrice>
{0}{0}{0}</ram:SpecifiedLineTradeAgreement>";
            SpecifiedLineTradeAgreement = String.Format(SpecifiedLineTradeAgreement
                , "\t"
                , item.LINE_BASIS_CURRENCY_CODE
                , item.PRODUCT_CHARGE_AMOUNT);
            #endregion

            #region SpecifiedLineTradeDelivery
            string SpecifiedLineTradeDelivery =
@"{0}{0}{0}<ram:SpecifiedLineTradeDelivery>
{0}{0}{0}{0}<ram:BilledQuantity unitCode=""{1}"">{2}</ram:BilledQuantity>
{0}{0}{0}</ram:SpecifiedLineTradeDelivery>";
            SpecifiedLineTradeDelivery = String.Format(SpecifiedLineTradeDelivery
                , "\t"
                , item.PRODUCT_UNIT_CODE
                , item.PRODUCT_QUANTITY);
            #endregion

            #region SpecifiedLineTradeSettlement
            #region ApplicableTradeTax
            string ApplicableTradeTax =
@"{0}{0}{0}{0}<ram:ApplicableTradeTax>
{0}{0}{0}{0}{0}<ram:TypeCode>{1}</ram:TypeCode>
{0}{0}{0}{0}{0}<ram:CalculatedRate>{2}</ram:CalculatedRate>
{0}{0}{0}{0}{0}<ram:BasisAmount currencyID=""{3}"">{4}</ram:BasisAmount>
{0}{0}{0}{0}{0}<ram:CalculatedAmount currencyID=""{3}"">{5}</ram:CalculatedAmount>
{0}{0}{0}{0}</ram:ApplicableTradeTax>";
            ApplicableTradeTax = String.Format(ApplicableTradeTax
                , "\t"
                , item.LINE_TAX_TYPE_CODE
                , item.LINE_TAX_CAL_RATE
                , item.LINE_BASIS_CURRENCY_CODE
                , item.LINE_BASIS_AMOUNT
                , item.LINE_TAX_CAL_AMOUNT);
            #endregion

            #region SpecifiedTradeAllowanceCharge
            string SpecifiedTradeAllowanceCharge =
@"{0}{0}{0}{0}<ram:SpecifiedTradeAllowanceCharge>
{0}{0}{0}{0}{0}<ram:ChargeIndicator>{1}</ram:ChargeIndicator>
{0}{0}{0}{0}{0}<ram:ActualAmount currencyID=""{2}"">{3}</ram:ActualAmount>
{0}{0}{0}{0}{0}<ram:TypeCode>95</ram:TypeCode> 
{0}{0}{0}{0}</ram:SpecifiedTradeAllowanceCharge>";
            SpecifiedTradeAllowanceCharge = String.Format(SpecifiedTradeAllowanceCharge
                , "\t"
                , item.LINE_ALLOWANCE_CHARGE_IND
                , item.LINE_BASIS_CURRENCY_CODE
                , item.LINE_ALLOWANCE_ACTUAL_AMOUNT);
            #endregion

            #region SpecifiedTradeSettlementLineMonetarySummation
            string SpecifiedTradeSettlementLineMonetarySummation =
@"{0}{0}{0}{0}<ram:SpecifiedTradeSettlementLineMonetarySummation>
{0}{0}{0}{0}{0}<ram:TaxTotalAmount currencyID=""{1}"">{2}</ram:TaxTotalAmount>
{0}{0}{0}{0}{0}<ram:NetLineTotalAmount currencyID=""{1}"">{3}</ram:NetLineTotalAmount>
{0}{0}{0}{0}{0}<ram:NetIncludingTaxesLineTotalAmount currencyID=""{1}"">{4}</ram:NetIncludingTaxesLineTotalAmount>
{0}{0}{0}{0}</ram:SpecifiedTradeSettlementLineMonetarySummation>";
            SpecifiedTradeSettlementLineMonetarySummation = String.Format(SpecifiedTradeSettlementLineMonetarySummation
                , "\t"
                , item.LINE_BASIS_CURRENCY_CODE
                , item.LINE_TAX_TOTAL_AMOUNT
                , item.LINE_NET_TOTAL_AMOUNT
                , item.LINE_NET_INCLUDE_TAX_TOTAL_AMOUNT);

            #endregion

            string SpecifiedLineTradeSettlement =
@"{0}{0}{0}<ram:SpecifiedLineTradeSettlement>
{1}
{2}
{3}
{0}{0}{0}</ram:SpecifiedLineTradeSettlement>";
            SpecifiedLineTradeSettlement = String.Format(SpecifiedLineTradeSettlement
                , "\t"
                , ApplicableTradeTax
                , SpecifiedTradeAllowanceCharge
                , SpecifiedTradeSettlementLineMonetarySummation);
            #endregion

            string IncludedSupplyChainTradeLineItem =
@"{0}{0}<ram:IncludedSupplyChainTradeLineItem>
{1}
{2}
{3}
{4}
{5}
{0}{0}</ram:IncludedSupplyChainTradeLineItem>";

            result = String.Format(IncludedSupplyChainTradeLineItem
                , "\t"
                , AssociatedDocumentLineDocument
                , SpecifiedTradeProduct
                , SpecifiedLineTradeAgreement
                , SpecifiedLineTradeDelivery
                , SpecifiedLineTradeSettlement);
            #endregion
            return result;
        }
        public string GetXMLLayout()
        {
            string tab = "\t";
            string ListXMLTradeLineItemInformation = "";
            foreach (var item in this.ListTradeLineItemInformation)
            {
                if (ListXMLTradeLineItemInformation != "")
                    ListXMLTradeLineItemInformation += Environment.NewLine;
                ListXMLTradeLineItemInformation += GetTradeLineItemInformation(item);
            }
            #region ExchangedDocumentContext
            string ExchangedDocumentContext =
@"{0}<rsm:ExchangedDocumentContext>
{0}{0}<ram:GuidelineSpecifiedDocumentContextParameter>
{0}{0}{0}<ram:ID schemeAgencyID=""ETDA"" schemeVersionID=""v2.0"">ER3-2560</ram:ID>
{0}{0}</ram:GuidelineSpecifiedDocumentContextParameter>
{0}</rsm:ExchangedDocumentContext>";
            ExchangedDocumentContext = String.Format(ExchangedDocumentContext, "\t");
            #endregion

            #region ExchangedDocument
            string ExchangedDocument =
@"{0}<rsm:ExchangedDocument>
{0}{0}<ram:ID>{1}</ram:ID>
{0}{0}<ram:Name>{2}</ram:Name>
{0}{0}<ram:TypeCode listID=""1001_ThaiDocumentNameCodeInvoice"" listAgencyID=""RD/ETDA"" listVersionID=""15A"">{3}</ram:TypeCode>
{0}{0}<ram:IssueDateTime>{4}</ram:IssueDateTime>
{0}{0}<ram:CreationDateTime>{5}</ram:CreationDateTime>
{0}{0}<ram:IncludedNote>
{0}{0}{0}<ram:Subject>DocumentRemark</ram:Subject>
{0}{0}{0}<ram:Content>{6}</ram:Content>
{0}{0}</ram:IncludedNote>
{0}</rsm:ExchangedDocument>";
            ExchangedDocument = String.Format(ExchangedDocument
                , "\t"
                , DocumentHeader.DOCUMENT_ID
                , DocumentHeader.DOCUMENT_NAME
                , DocumentHeader.DOCUMENT_TYPE_CODE
                , DocumentHeader.DOCUMENT_ISSUE_DTM
                , DocumentHeader.ConvertISODateTime(DateTime.Now)
                , DocumentHeader.GetStringXML(DocumentHeader.DOCUMENT_REMARK));
            #endregion

            #region SupplyChainTradeTransaction
            #region ApplicableHeaderTradeAgreement
            #region SellerTradeParty
            #region Seller_PostalTradeAddress
            string Seller_PostalTradeAddress = "";
            if (this.SellerInformation.SELLER_ADDRESS_LINE1 != "")
            {
                Seller_PostalTradeAddress =
@"{0}{0}{0}{0}{0}<ram:LineOne>{1}</ram:LineOne>
{0}{0}{0}{0}{0}<ram:LineTwo>{2}</ram:LineTwo>
{0}{0}{0}{0}{0}<ram:CountryID>{3}</ram:CountryID>";
                Seller_PostalTradeAddress = String.Format(Seller_PostalTradeAddress
                    , "\t"
                    , this.SellerInformation.SELLER_ADDRESS_LINE1
                    , this.SellerInformation.SELLER_ADDRESS_LINE2
                    , this.SellerInformation.SELLER_COUNTRY_ID);
            }
            else
            {
                Seller_PostalTradeAddress =
@"{0}{0}{0}{0}{0}<ram:PostcodeCode>{1}</ram:PostcodeCode>
{0}{0}{0}{0}{0}{2}
{0}{0}{0}{0}{0}{3}
{0}{0}{0}{0}{0}{4}
{0}{0}{0}{0}{0}{5}
{0}{0}{0}{0}{0}{6}
{0}{0}{0}{0}{0}<ram:CityName>{7}</ram:CityName>
{0}{0}{0}{0}{0}<ram:CitySubDivisionName>{8}</ram:CitySubDivisionName>
{0}{0}{0}{0}{0}<ram:CountryID>{9}</ram:CountryID>
{0}{0}{0}{0}{0}<ram:CountrySubDivisionID>{10}</ram:CountrySubDivisionID>
{0}{0}{0}{0}{0}{11}";

                Seller_PostalTradeAddress = String.Format(Seller_PostalTradeAddress
                    , "\t"
                    , this.SellerInformation.SELLER_POST_CODE
                    , this.SellerInformation.SELLER_BUILDING_NAME == "" ? "" : String.Format("<ram:BuildingName>{0}</ram:BuildingName>", this.SellerInformation.SELLER_BUILDING_NAME)
                    , this.SellerInformation.SELLER_ADDRESS_LINE3 == "" ? "" : String.Format("<ram:LineThree>{0}</ram:LineThree>", this.SellerInformation.SELLER_ADDRESS_LINE3)
                    , this.SellerInformation.SELLER_ADDRESS_LINE4 == "" ? "" : String.Format("<ram:LineFour>{0}</ram:LineFour>", this.SellerInformation.SELLER_ADDRESS_LINE4)
                    , this.SellerInformation.SELLER_ADDRESS_LINE5 == "" ? "" : String.Format("<ram:LineFive>{0}</ram:LineFive>", this.SellerInformation.SELLER_ADDRESS_LINE5)
                    , this.SellerInformation.SELLER_STREET_NAME == "" ? "" : String.Format("<ram:StreetName>{0}</ram:StreetName>", this.SellerInformation.SELLER_STREET_NAME)
                    , this.SellerInformation.SELLER_CITY_ID
                    , this.SellerInformation.SELLER_CITY_SUB_DIV_ID
                    , this.SellerInformation.SELLER_COUNTRY_ID
                    , this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID
                    , this.SellerInformation.SELLER_BUILDING_NO == "" ? "" : String.Format("<ram:BuildingNumber>{0}</ram:BuildingNumber>", this.SellerInformation.SELLER_BUILDING_NO));
            }
            #endregion

            string SellerTradeParty =
@"{0}{0}{0}<ram:SellerTradeParty>
{0}{0}{0}{0}<ram:Name>{1}</ram:Name>
{0}{0}{0}{0}<ram:SpecifiedTaxRegistration>
{0}{0}{0}{0}{0}<ram:ID schemeID=""TXID"">{2}</ram:ID>
{0}{0}{0}{0}</ram:SpecifiedTaxRegistration>
{0}{0}{0}{0}<ram:PostalTradeAddress>
{3}
{0}{0}{0}{0}</ram:PostalTradeAddress>
{0}{0}{0}</ram:SellerTradeParty>";
            SellerTradeParty = String.Format(SellerTradeParty
                , "\t"
                , this.SellerInformation.SELLER_NAME
                , this.SellerInformation.SELLER_TAX_ID + this.SellerInformation.SELLER_BRANCH_ID
                , Seller_PostalTradeAddress);
            #endregion

            #region BuyerTradeParty
            #region DefinedTradeContact
            string DefinedTradeContact = "";
            // ตัดช่องว่างหัวท้ายก่อนเช็ค กันอีเมลลูกค้าที่มีแต่ช่องว่างทำให้ tag URIID ว่าง (schema กรมสรรพากรบังคับอย่างน้อย 1 ตัวอักษร)
            string BuyerURIID = (BuyerInformation.BUYER_URIID ?? "").Trim();
            if (BuyerURIID != "")
            {
                DefinedTradeContact =
@"{0}{0}{0}{0}<ram:DefinedTradeContact>
{0}{0}{0}{0}{0}<ram:EmailURIUniversalCommunication>
{0}{0}{0}{0}{0}{0}<ram:URIID>{1}</ram:URIID>
{0}{0}{0}{0}{0}</ram:EmailURIUniversalCommunication>
{0}{0}{0}{0}</ram:DefinedTradeContact>";
                DefinedTradeContact = String.Format(DefinedTradeContact
                    , "\t"
                    , BuyerURIID);
            }
            #endregion

            #region Buyer_PostalTradeAddress
            string Buyer_PostalTradeAddress = "";
            if (this.BuyerInformation.BUYER_ADDRESS_LINE1 != "")
            {
                Buyer_PostalTradeAddress =
$@"{tab}{tab}{tab}{tab}{tab}<ram:PostcodeCode>{this.BuyerInformation.BUYER_POST_CODE}</ram:PostcodeCode>
{tab}{tab}{tab}{tab}{tab}<ram:LineOne>{this.BuyerInformation.BUYER_ADDRESS_LINE1}</ram:LineOne>
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_ADDRESS_LINE2 == "" ? "" : $"<ram:LineTwo>{this.BuyerInformation.BUYER_ADDRESS_LINE2}</ram:LineTwo>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_CITY_ID == "" ? "" : $"<ram:CityName>{this.BuyerInformation.BUYER_CITY_ID}</ram:CityName>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_CITY_SUB_DIV_ID == "" ? "" : $"<ram:CitySubDivisionName>{this.BuyerInformation.BUYER_CITY_SUB_DIV_ID}</ram:CitySubDivisionName>")}
{tab}{tab}{tab}{tab}{tab}<ram:CountryID>{this.BuyerInformation.BUYER_COUNTRY_ID}</ram:CountryID>
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID == "" ? "" : $"<ram:CountrySubDivisionID>{this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID}</ram:CountrySubDivisionID>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_BUILDING_NO == "" ? "" : $"<ram:BuildingNumber>{this.BuyerInformation.BUYER_BUILDING_NO}</ram:BuildingNumber>")}";
            }
            else
            {
                Buyer_PostalTradeAddress =
$@"{tab}{tab}{tab}{tab}{tab}<ram:PostcodeCode>{this.BuyerInformation.BUYER_POST_CODE}</ram:PostcodeCode>
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_BUILDING_NAME == "" ? "" : $"<ram:BuildingName>{this.BuyerInformation.BUYER_BUILDING_NAME}</ram:BuildingName>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_ADDRESS_LINE3 == "" ? "" : $"<ram:LineThree>{this.BuyerInformation.BUYER_ADDRESS_LINE3}</ram:LineThree>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_ADDRESS_LINE4 == "" ? "" : $"<ram:LineFour>{this.BuyerInformation.BUYER_ADDRESS_LINE4}</ram:LineFour>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_ADDRESS_LINE5 == "" ? "" : $"<ram:LineFive>{this.BuyerInformation.BUYER_ADDRESS_LINE5}</ram:LineFive>")}
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_STREET_NAME == "" ? "" : $"<ram:StreetName>{this.BuyerInformation.BUYER_STREET_NAME}</ram:StreetName>")}
{tab}{tab}{tab}{tab}{tab}<ram:CityName>{this.BuyerInformation.BUYER_CITY_ID}</ram:CityName>
{tab}{tab}{tab}{tab}{tab}<ram:CitySubDivisionName>{this.BuyerInformation.BUYER_CITY_SUB_DIV_ID}</ram:CitySubDivisionName>
{tab}{tab}{tab}{tab}{tab}<ram:CountryID>{this.BuyerInformation.BUYER_COUNTRY_ID}</ram:CountryID>
{tab}{tab}{tab}{tab}{tab}<ram:CountrySubDivisionID>{this.BuyerInformation.BUYER_COUNTRY_SUB_DIV_ID}</ram:CountrySubDivisionID>
{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_BUILDING_NO == "" ? "" : $"<ram:BuildingNumber>{this.BuyerInformation.BUYER_BUILDING_NO}</ram:BuildingNumber>")}";
            }
            #endregion

            string BuyerTradeParty =
@"{0}{0}{0}<ram:BuyerTradeParty>
{0}{0}{0}{0}<ram:ID>{1}</ram:ID>
{0}{0}{0}{0}<ram:Name>{2}</ram:Name>
{0}{0}{0}{0}<ram:SpecifiedTaxRegistration>
{0}{0}{0}{0}{0}<ram:ID schemeID=""{3}"">{4}</ram:ID>
{0}{0}{0}{0}</ram:SpecifiedTaxRegistration>
{5}
{0}{0}{0}{0}<ram:PostalTradeAddress>
{6}
{0}{0}{0}{0}</ram:PostalTradeAddress>
{0}{0}{0}</ram:BuyerTradeParty>";
            BuyerTradeParty = String.Format(BuyerTradeParty
                , "\t"
                , this.BuyerInformation.BUYER_ID
                , this.BuyerInformation.BUYER_NAME
                , this.BuyerInformation.BUYER_TAX_ID_TYPE
                , this.BuyerInformation.BUYER_TAX_ID + this.BuyerInformation.BUYER_BRANCH_ID
                , DefinedTradeContact
                , Buyer_PostalTradeAddress);
            #endregion

            #region BuyerOrderReferencedDocument
            string BuyerOrderReferencedDocument = "";
            if (DocumentHeader.BUYER_ORDER_ASSIGN_ID != "")
            {
                BuyerOrderReferencedDocument =
@"{0}{0}{0}<ram:BuyerOrderReferencedDocument>
{0}{0}{0}{0}<ram:IssuerAssignedID>{1}</ram:IssuerAssignedID>
{0}{0}{0}</ram:BuyerOrderReferencedDocument>";
                BuyerOrderReferencedDocument = String.Format(BuyerOrderReferencedDocument
                    , "\t"
                    , DocumentHeader.BUYER_ORDER_ASSIGN_ID);
            }
            #endregion
            string ApplicableHeaderTradeAgreement =
@"{0}{0}<ram:ApplicableHeaderTradeAgreement>
{1}
{2}
{3}
{0}{0}</ram:ApplicableHeaderTradeAgreement>";
            ApplicableHeaderTradeAgreement = String.Format(ApplicableHeaderTradeAgreement
                , "\t"
                , SellerTradeParty
                , BuyerTradeParty
                , BuyerOrderReferencedDocument);
            #endregion

            #region ApplicableHeaderTradeDelivery
            string ApplicableHeaderTradeDelivery =
@"{0}{0}<ram:ApplicableHeaderTradeDelivery>
{0}{0}{0}<ram:ActualDeliverySupplyChainEvent>
{0}{0}{0}{0}<ram:OccurrenceDateTime>{1}</ram:OccurrenceDateTime>
{0}{0}{0}</ram:ActualDeliverySupplyChainEvent>
{0}{0}</ram:ApplicableHeaderTradeDelivery>";
            ApplicableHeaderTradeDelivery = String.Format(ApplicableHeaderTradeDelivery
                , "\t"
                , DocumentFooter.DELIVERY_OCCUR_DTM);
            #endregion

            #region ApplicableHeaderTradeSettlement
            #region InvoiceCurrencyCode
            string InvoiceCurrencyCode =
@"{0}{0}{0}<ram:InvoiceCurrencyCode>{1}</ram:InvoiceCurrencyCode>";
            InvoiceCurrencyCode = String.Format(InvoiceCurrencyCode
                , "\t"
                , DocumentFooter.INVOICE_CURRENCY_CODE);
            #endregion

            #region ApplicableTradeTax
            string ApplicableTradeTax =
@"{0}{0}{0}<ram:ApplicableTradeTax>
{0}{0}{0}{0}<ram:TypeCode>{1}</ram:TypeCode>
{0}{0}{0}{0}<ram:CalculatedRate>{2}</ram:CalculatedRate>
{0}{0}{0}{0}<ram:BasisAmount currencyID=""{3}"">{4}</ram:BasisAmount>
{0}{0}{0}{0}<ram:CalculatedAmount currencyID=""{3}"">{5}</ram:CalculatedAmount>
{0}{0}{0}</ram:ApplicableTradeTax>";
            ApplicableTradeTax = String.Format(ApplicableTradeTax
                , "\t"
                , DocumentFooter.TAX_TYPE_CODE
                , DocumentFooter.TAX_CAL_RATE
                , DocumentFooter.INVOICE_CURRENCY_CODE
                , DocumentFooter.BASIS_AMOUNT
                , DocumentFooter.TAX_CAL_AMOUNT);
            #endregion

            #region SpecifiedTradeAllowanceCharge
            string SpecifiedTradeAllowanceCharge =
@"{0}{0}{0}<ram:SpecifiedTradeAllowanceCharge>
{0}{0}{0}{0}<ram:ChargeIndicator>{1}</ram:ChargeIndicator>
{0}{0}{0}{0}<ram:ActualAmount currencyID=""{2}"">{3}</ram:ActualAmount>
{0}{0}{0}{0}<ram:TypeCode>95</ram:TypeCode> 
{0}{0}{0}</ram:SpecifiedTradeAllowanceCharge>";
            SpecifiedTradeAllowanceCharge = String.Format(SpecifiedTradeAllowanceCharge
                , "\t"
                , DocumentFooter.ALLOWANCE_CHARGE_IND
                , DocumentFooter.ALLOWANCE_TOTAL_CURRENCY_CODE
                , DocumentFooter.ALLOWANCE_ACTUAL_AMOUNT);

            #endregion

            #region SpecifiedTradeSettlementHeaderMonetarySummation
            string SpecifiedTradeSettlementHeaderMonetarySummation =
@"{0}{0}{0}<ram:SpecifiedTradeSettlementHeaderMonetarySummation>
{0}{0}{0}{0}<ram:LineTotalAmount currencyID=""{1}"">{2}</ram:LineTotalAmount>
{0}{0}{0}{0}<ram:AllowanceTotalAmount currencyID=""{1}"">{3}</ram:AllowanceTotalAmount>
{0}{0}{0}{0}<ram:TaxBasisTotalAmount currencyID=""{1}"">{4}</ram:TaxBasisTotalAmount>
{0}{0}{0}{0}<ram:TaxTotalAmount currencyID=""{1}"">{5}</ram:TaxTotalAmount>
{0}{0}{0}{0}<ram:GrandTotalAmount currencyID=""{1}"">{6}</ram:GrandTotalAmount>
{0}{0}{0}</ram:SpecifiedTradeSettlementHeaderMonetarySummation>";
            SpecifiedTradeSettlementHeaderMonetarySummation = String.Format(SpecifiedTradeSettlementHeaderMonetarySummation
                , "\t"
                , DocumentFooter.TAX_TOTAL_CURRENCY_CODE
                , DocumentFooter.LINE_TOTAL_AMOUNT
                , DocumentFooter.ALLOWANCE_TOTAL_AMOUNT
                , DocumentFooter.TAX_BASIS_TOTAL_AMOUNT
                , DocumentFooter.TAX_TOTAL_AMOUNT
                , DocumentFooter.GRAND_TOTAL_AMOUNT);
            #endregion

            string ApplicableHeaderTradeSettlement =
@"{0}{0}<ram:ApplicableHeaderTradeSettlement>
{1}
{2}
{3}
{4}
{0}{0}</ram:ApplicableHeaderTradeSettlement>";
            ApplicableHeaderTradeSettlement = String.Format(ApplicableHeaderTradeSettlement
                , "\t"
                , InvoiceCurrencyCode
                , ApplicableTradeTax
                , SpecifiedTradeAllowanceCharge
                , SpecifiedTradeSettlementHeaderMonetarySummation);
            #endregion

            string SupplyChainTradeTransaction =
@"{0}<rsm:SupplyChainTradeTransaction>
{1}
{2}
{3}
{4}
{0}</rsm:SupplyChainTradeTransaction>";
            SupplyChainTradeTransaction = String.Format(SupplyChainTradeTransaction
                , "\t"
                , ApplicableHeaderTradeAgreement
                , ApplicableHeaderTradeDelivery
                , ApplicableHeaderTradeSettlement
                , ListXMLTradeLineItemInformation);
            #endregion

            string result =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<?template name=""CCD001"" xslt=""V10000""?>
<?xml-model href=""Receipt_Schematron_2p0.sch"" type=""application/xml"" schematypens=""http://purl.oclc.org/dsdl/schematron""?>
<rsm:Receipt_CrossIndustryInvoice xmlns:rsm=""urn:etda:uncefact:data:standard:Receipt_CrossIndustryInvoice:2"" xmlns:ram=""urn:etda:uncefact:data:standard:Receipt_ReusableAggregateBusinessInformationEntity:2"">
{0}
{1}
{2}
</rsm:Receipt_CrossIndustryInvoice>";
            result = String.Format(result, ExchangedDocumentContext, ExchangedDocument, SupplyChainTradeTransaction);
            return result;
        }
    }
}
