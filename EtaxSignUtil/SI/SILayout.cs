using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtaxSignUtil.Layout;
using DBUtil;
using System.Data;
using ModuleUtil;
using System.Globalization;

namespace EtaxSignUtil.SI
{
    public class SILayout : BaseLayout
    {
        protected FileControl FileControl { get; set; }
        public SI.DocumentHeader DocumentHeader { get; set; }
        public BuyerInformation BuyerInformation { get; set; }
        public List<TradeLineItemInformation> ListTradeLineItemInformation { get; set; }
        public SI.DocumentFooter DocumentFooter { get; set; }
        public FileTrailer FileTrailer { get; set; }
        public SILayout(DBSimple dbSimple, QEB.Center QEBCenterInfo)
            : base(dbSimple, QEBCenterInfo)
        {

        }
        public virtual int InitLayout(DataRow rowHeader, ref string ErrMsg)
        {
            int TransErr = 0;
            int ErrCode = 0;
            bool val = this.GetSellerInfomation(ref ErrMsg);
            if (!val)
                return ++TransErr;
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowHeader);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowHeader, 0);
            string QueryHeader =
$@"SELECT SI.InvoiceNo
,SI.InvoiceDate
,SI.PORefNo
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
,SI.TotalAmountIncludeVATTrueFalse
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
WHERE SI.DocumentTypeCode = '{DocumentTypeCode}'
AND SI.RunNo = {RunNo}";
            DataTable TBHeader = new DataTable();
            TransErr = dbSimple.FillData(TBHeader, QueryHeader, ref ErrCode, ref ErrMsg);
            if (TransErr != 0 || TBHeader.Rows.Count == 0)
            {
                ErrMsg = "No Header Select";
                return ++TransErr;
            }

            string QueryDetails =
$@"SELECT TransactionCode
, TransactionDescription
, Quantity
, SalesUnitCode
, UnitPriceCurrency
, TotalAmountCurrency
, TotalAmountAfterDiscount
, Note
FROM SalesInvoiceDetails
WHERE DocumentTypeCode = '{DocumentTypeCode}'
AND RunNo = {RunNo}
AND TransactionType IN ('I','B','F')
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

            this.DocumentHeader = new SI.DocumentHeader(InvoiceNo
             , InvoiceDate
             , RefPONo
             , Remark);

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
            DataRow[] listItemDiscount = TBDetails.AsEnumerable().Where(x => x.Field<double>("TotalAmountAfterDiscount") < 0).ToArray();
            foreach (DataRow rowSIDetailsUpdate in listItemDiscount)
            {
                double TotalAmountAfterDiscount = Math.Abs(ReceiveValue.DoubleReceive("TotalAmountAfterDiscount", rowSIDetailsUpdate, 0));
                AmountDiscountCurrency += TotalAmountAfterDiscount;
                AmountDiscountCurrency = ModuleMath.RoundingDigit(AmountDiscountCurrency, 2);

            }
            int index = 0;
            double TotalDisCount = 0;
            this.ListTradeLineItemInformation = new List<TradeLineItemInformation>();
            DataRow rowLast = TBDetails.AsEnumerable().LastOrDefault(x => x.Field<double>("TotalAmountAfterDiscount") > 0);
            DataRow[] listItem = TBDetails.AsEnumerable().Where(x => x.Field<double>("TotalAmountAfterDiscount") >= 0).ToArray();
            var cal = new CalculateItem(AmountDiscountCurrency, TotalAmountCurrencyAfterDiscountBeforeVAT, TotalAmountCurrencyAfterVAT);
            foreach (DataRow rowSIDetailsUpdate in listItem)
            {
                index++;
                string TransactionCode = ReceiveValue.StringReceive("TransactionCode", rowSIDetailsUpdate);
                string TransactionDescription = ReceiveValue.StringReceive("TransactionDescription", rowSIDetailsUpdate);
                TransactionDescription = this.ReplaceNonUnicodeCharacters(TransactionDescription);
                double Quantity = ReceiveValue.DoubleReceive("Quantity", rowSIDetailsUpdate, 0);
                string SalesUnitCode = ReceiveValue.StringReceive("SalesUnitCode", rowSIDetailsUpdate);
                double UnitPriceCurrency = ReceiveValue.DoubleReceive("UnitPriceCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountCurrencyDetails = ReceiveValue.DoubleReceive("TotalAmountCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountAfterDiscount = ReceiveValue.DoubleReceive("TotalAmountAfterDiscount", rowSIDetailsUpdate, 0);
                string Note = ReceiveValue.StringReceive("Note", rowSIDetailsUpdate);
                Note = this.ReplaceNonUnicodeCharacters(Note);
                bool IsLast = (rowLast == rowSIDetailsUpdate || listItem.Length == 1) ? true : false;
                TradeLineItemInformation TradeLineItemInformation = new TradeLineItemInformation(index
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
                    , IsLast);

                TotalDisCount += Convert.ToDouble(TradeLineItemInformation.PRODUCT_ALLOWANCE_ACTUAL_AMOUNT);
                TotalDisCount = Math.Round(TotalDisCount, 2);

                this.ListTradeLineItemInformation.Add(TradeLineItemInformation);
            }
            foreach (DataRow rowSIDetailsUpdate in listItemDiscount)
            {
                index++;
                string TransactionCode = ReceiveValue.StringReceive("TransactionCode", rowSIDetailsUpdate);
                string TransactionDescription = ReceiveValue.StringReceive("TransactionDescription", rowSIDetailsUpdate);
                TransactionDescription = this.ReplaceNonUnicodeCharacters(TransactionDescription);
                double Quantity = ReceiveValue.DoubleReceive("Quantity", rowSIDetailsUpdate, 0);
                string SalesUnitCode = ReceiveValue.StringReceive("SalesUnitCode", rowSIDetailsUpdate);
                double UnitPriceCurrency = ReceiveValue.DoubleReceive("UnitPriceCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountCurrencyDetails = ReceiveValue.DoubleReceive("TotalAmountCurrency", rowSIDetailsUpdate, 0);
                double TotalAmountAfterDiscount = ReceiveValue.DoubleReceive("TotalAmountAfterDiscount", rowSIDetailsUpdate, 0);
                string Note = ReceiveValue.StringReceive("Note", rowSIDetailsUpdate);
                Note = this.ReplaceNonUnicodeCharacters(Note);
                TradeLineItemInformation TradeLineItemInformation = new TradeLineItemInformation(index
                    , TransactionCode
                    , TransactionDescription
                    , Quantity
                    , SalesUnitCode
                    , CurrencyCode
                    , TotalAmountIncludeVATTrueFalse
                    , VATRate
                    , TotalAmountAfterDiscount
                    , Note);
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
        public virtual int ValidateLayout(ref string ErrMsg)
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
            #region IncludedSupplyChainTradeLineItem
            string tab = "\t";
            #region IncludedSupplyChainTradeLineItem
            string AssociatedDocumentLineDocument =
$@"{tab}{tab}{tab}<ram:AssociatedDocumentLineDocument>
{tab}{tab}{tab}{tab}<ram:LineID>{item.LINE_ID}</ram:LineID>
{tab}{tab}{tab}</ram:AssociatedDocumentLineDocument>";
            #endregion

            #region SpecifiedTradeProduct
            #region InformationNote
            string SpecifiedTradeProduct_InformationNote = "";
            if (item.PRODUCT_REMARK != string.Empty)
            {
                SpecifiedTradeProduct_InformationNote =
$@"{tab}{tab}{tab}{tab}<ram:InformationNote>
{tab}{tab}{tab}{tab}{tab}<ram:Subject>ProductRemark</ram:Subject>
{tab}{tab}{tab}{tab}{tab}<ram:Content>{item.PRODUCT_REMARK}</ram:Content>
{tab}{tab}{tab}{tab}</ram:InformationNote>";
            }
            #endregion
            string SpecifiedTradeProduct =
$@"{tab}{tab}{tab}<ram:SpecifiedTradeProduct>
{tab}{tab}{tab}{tab}<ram:ID>{item.PRODUCT_ID}</ram:ID>
{tab}{tab}{tab}{tab}<ram:Name>{ item.PRODUCT_NAME}</ram:Name>
{SpecifiedTradeProduct_InformationNote}
{tab}{tab}{tab}</ram:SpecifiedTradeProduct>";
            #endregion

            #region SpecifiedLineTradeAgreement
            string SpecifiedLineTradeAgreement =
$@"{tab}{tab}{tab}<ram:SpecifiedLineTradeAgreement>
{tab}{tab}{tab}{tab}<ram:GrossPriceProductTradePrice>
{tab}{tab}{tab}{tab}{tab}<ram:ChargeAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.PRODUCT_CHARGE_AMOUNT}</ram:ChargeAmount>
{tab}{tab}{tab}{tab}</ram:GrossPriceProductTradePrice>
{tab}{tab}{tab}</ram:SpecifiedLineTradeAgreement>";
            #endregion

            #region SpecifiedLineTradeDelivery
            string SpecifiedLineTradeDelivery =
$@"{tab}{tab}{tab}<ram:SpecifiedLineTradeDelivery>
{tab}{tab}{tab}{tab}<ram:BilledQuantity unitCode=""{ item.PRODUCT_UNIT_CODE}"">{item.PRODUCT_QUANTITY}</ram:BilledQuantity>
{tab}{tab}{tab}</ram:SpecifiedLineTradeDelivery>";
            #endregion

            #region SpecifiedLineTradeSettlement
            #region ApplicableTradeTax
            string ApplicableTradeTax =
$@"{tab}{tab}{tab}{tab}<ram:ApplicableTradeTax>
{tab}{tab}{tab}{tab}{tab}<ram:TypeCode>{item.LINE_TAX_TYPE_CODE}</ram:TypeCode>
{tab}{tab}{tab}{tab}{tab}<ram:CalculatedRate>{item.LINE_TAX_CAL_RATE}</ram:CalculatedRate>
{tab}{tab}{tab}{tab}{tab}<ram:BasisAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_BASIS_AMOUNT}</ram:BasisAmount>
{tab}{tab}{tab}{tab}{tab}<ram:CalculatedAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_TAX_CAL_AMOUNT}</ram:CalculatedAmount>
{tab}{tab}{tab}{tab}</ram:ApplicableTradeTax>";
            #endregion

            #region SpecifiedTradeAllowanceCharge
            string SpecifiedTradeAllowanceCharge =
$@"{tab}{tab}{tab}{tab}<ram:SpecifiedTradeAllowanceCharge>
{tab}{tab}{tab}{tab}{tab}<ram:ChargeIndicator>{item.LINE_ALLOWANCE_CHARGE_IND}</ram:ChargeIndicator>
{tab}{tab}{tab}{tab}{tab}<ram:ActualAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_ALLOWANCE_ACTUAL_AMOUNT}</ram:ActualAmount>
{tab}{tab}{tab}{tab}{tab}<ram:TypeCode>{item.LINE_ALLOWANCE_TYPE_CODE}</ram:TypeCode> 
{tab}{tab}{tab}{tab}</ram:SpecifiedTradeAllowanceCharge>";
            #endregion

            #region SpecifiedTradeSettlementLineMonetarySummation
            string SpecifiedTradeSettlementLineMonetarySummation =
$@"{tab}{tab}{tab}{tab}<ram:SpecifiedTradeSettlementLineMonetarySummation>
{tab}{tab}{tab}{tab}{tab}<ram:TaxTotalAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_TAX_TOTAL_AMOUNT}</ram:TaxTotalAmount>
{tab}{tab}{tab}{tab}{tab}<ram:NetLineTotalAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_NET_TOTAL_AMOUNT}</ram:NetLineTotalAmount>
{tab}{tab}{tab}{tab}{tab}<ram:NetIncludingTaxesLineTotalAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_NET_INCLUDE_TAX_TOTAL_AMOUNT}</ram:NetIncludingTaxesLineTotalAmount>
{tab}{tab}{tab}{tab}</ram:SpecifiedTradeSettlementLineMonetarySummation>";
            #endregion

            string SpecifiedLineTradeSettlement =
$@"{tab}{tab}{tab}<ram:SpecifiedLineTradeSettlement>
{ApplicableTradeTax}
{SpecifiedTradeAllowanceCharge}
{SpecifiedTradeSettlementLineMonetarySummation}
{tab}{tab}{tab}</ram:SpecifiedLineTradeSettlement>";
            #endregion

            string IncludedSupplyChainTradeLineItem =
$@"{tab}{tab}<ram:IncludedSupplyChainTradeLineItem>
{AssociatedDocumentLineDocument}
{SpecifiedTradeProduct}
{SpecifiedLineTradeAgreement}
{SpecifiedLineTradeDelivery}
{SpecifiedLineTradeSettlement}
{tab}{tab}</ram:IncludedSupplyChainTradeLineItem>";
            #endregion
            return IncludedSupplyChainTradeLineItem;
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
$@"{tab}<rsm:ExchangedDocumentContext>
{tab}{tab}<ram:GuidelineSpecifiedDocumentContextParameter>
{tab}{tab}{tab}<ram:ID schemeAgencyID=""1"" schemeVersionID=""v2.0"">1</ram:ID>
{tab}{tab}</ram:GuidelineSpecifiedDocumentContextParameter>
{tab}</rsm:ExchangedDocumentContext>";
            #endregion

            #region ExchangedDocument
            string ExchangedDocument =
$@"{tab}<rsm:ExchangedDocument>
{tab}{tab}<ram:ID>{DocumentHeader.DOCUMENT_ID}</ram:ID>
{tab}{tab}<ram:Name>{DocumentHeader.DOCUMENT_NAME}</ram:Name>
{tab}{tab}<ram:TypeCode listID=""1001_ThaiDocumentNameCodeInvoice"" listAgencyID=""RD/ETDA"" listVersionID=""15A"">{DocumentHeader.DOCUMENT_TYPE_CODE}</ram:TypeCode>
{tab}{tab}<ram:IssueDateTime>{DocumentHeader.DOCUMENT_ISSUE_DTM}</ram:IssueDateTime>
{tab}{tab}<ram:CreationDateTime>{DocumentHeader.ConvertISODateTime(DateTime.Now)}</ram:CreationDateTime>
{tab}{tab}<ram:IncludedNote>
{tab}{tab}{tab}<ram:Subject>DocumentRemark</ram:Subject>
{tab}{tab}{tab}<ram:Content>{DocumentHeader.GetStringXML(DocumentHeader.DOCUMENT_REMARK)}</ram:Content>
{tab}{tab}</ram:IncludedNote>
{tab}{tab}<ram:IncludedNote>
{tab}{tab}{tab}<ram:Subject>DueDateTime</ram:Subject>
{tab}{tab}{tab}<ram:Content>{DocumentFooter.PAYMENT_DUE_DTM}</ram:Content>
{tab}{tab}</ram:IncludedNote>
{tab}</rsm:ExchangedDocument>";
            #endregion

            #region SupplyChainTradeTransaction
            #region ApplicableHeaderTradeAgreement
            #region SellerTradeParty
            #region Seller_PostalTradeAddress
            string Seller_PostalTradeAddress = "";
            if (this.SellerInformation.SELLER_ADDRESS_LINE1 != "" && this.SellerInformation.SELLER_ADDRESS_LINE2 != "")
            {
                Seller_PostalTradeAddress =
@"{0}{0}{0}{0}{0}<ram:PostcodeCode>{1}</ram:PostcodeCode>
{0}{0}{0}{0}{0}<ram:LineOne>{2}</ram:LineOne>
{0}{0}{0}{0}{0}{3}
{0}{0}{0}{0}{0}<ram:CountryID>{4}</ram:CountryID>
{0}{0}{0}{0}{0}<ram:CountrySubDivisionID>{5}</ram:CountrySubDivisionID>
{0}{0}{0}{0}{0}<ram:BuildingNumber>{6}</ram:BuildingNumber>";
                Seller_PostalTradeAddress = String.Format(Seller_PostalTradeAddress
                    , "\t"
                    , this.SellerInformation.SELLER_POST_CODE
                    , this.SellerInformation.SELLER_ADDRESS_LINE1
                    , this.SellerInformation.SELLER_ADDRESS_LINE2 == "" ? "" : String.Format("<ram:LineTwo>{0}</ram:LineTwo>", this.SellerInformation.SELLER_ADDRESS_LINE2)
                    , this.SellerInformation.SELLER_COUNTRY_ID
                    , this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID
                    , this.SellerInformation.SELLER_BUILDING_NO);
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
$@"{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_POST_CODE == "" ? "" : $"<ram:PostcodeCode>{this.BuyerInformation.BUYER_POST_CODE}</ram:PostcodeCode>")}
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
$@"{tab}{tab}{tab}{tab}{tab}{(this.BuyerInformation.BUYER_POST_CODE == "" ? "" : $"<ram:PostcodeCode>{this.BuyerInformation.BUYER_POST_CODE}</ram:PostcodeCode>")}
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
$@"{tab}{tab}{tab}<ram:BuyerOrderReferencedDocument>
{tab}{tab}{tab}{tab}<ram:IssuerAssignedID>{DocumentHeader.BUYER_ORDER_ASSIGN_ID}</ram:IssuerAssignedID>
{tab}{tab}{tab}</ram:BuyerOrderReferencedDocument>";
            }
            #endregion
            string ApplicableHeaderTradeAgreement =
$@"{tab}{tab}<ram:ApplicableHeaderTradeAgreement>
{SellerTradeParty}
{BuyerTradeParty}
{BuyerOrderReferencedDocument}
{tab}{tab}</ram:ApplicableHeaderTradeAgreement>";
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

            #region SpecifiedTradePaymentTerms
            string SpecifiedTradePaymentTerms =
@"{0}{0}{0}<ram:SpecifiedTradePaymentTerms>
{0}{0}{0}{0}<ram:Description>{1}</ram:Description>
{0}{0}{0}{0}<ram:DueDateDateTime>{2}</ram:DueDateDateTime>
{0}{0}{0}{0}<ram:TypeCode>{3}</ram:TypeCode>
{0}{0}{0}</ram:SpecifiedTradePaymentTerms>";
            SpecifiedTradePaymentTerms = String.Format(SpecifiedTradePaymentTerms
                , "\t"
                , DocumentFooter.PAYMENT_DESCRIPTION
                , DocumentFooter.PAYMENT_DUE_DTM
                , DocumentFooter.PAYMENT_TYPE_CODE);
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
{5}
{0}{0}</ram:ApplicableHeaderTradeSettlement>";
            ApplicableHeaderTradeSettlement = String.Format(ApplicableHeaderTradeSettlement
                , "\t"
                , InvoiceCurrencyCode
                , ApplicableTradeTax
                , SpecifiedTradeAllowanceCharge
                , SpecifiedTradePaymentTerms
                , SpecifiedTradeSettlementHeaderMonetarySummation);
            #endregion

            string SupplyChainTradeTransaction =
$@"{tab}<rsm:SupplyChainTradeTransaction>
{ApplicableHeaderTradeAgreement}
{ApplicableHeaderTradeDelivery}
{ApplicableHeaderTradeSettlement}
{ListXMLTradeLineItemInformation}
{tab}</rsm:SupplyChainTradeTransaction>";
            #endregion

            string result =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<?template name=""CCD001"" xslt=""V10000""?>
<?xml-model href=""TaxInvoice_Schematron_2p0.sch"" type=""application/xml"" schematypens=""http://purl.oclc.org/dsdl/schematron""?>
<rsm:TaxInvoice_CrossIndustryInvoice xmlns:rsm=""urn:etda:uncefact:data:standard:TaxInvoice_CrossIndustryInvoice:2"" xmlns:ram=""urn:etda:uncefact:data:standard:TaxInvoice_ReusableAggregateBusinessInformationEntity:2"">
{ExchangedDocumentContext}
{ExchangedDocument}
{SupplyChainTradeTransaction}
</rsm:TaxInvoice_CrossIndustryInvoice>";
            return result;
        }
        private string ReplaceNonUnicodeCharacters(string input)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in input)
            {
                if (IsUnicodeCharacter(c))
                {
                    result.Append(c);
                }
                else
                {
                    // Replace non-Unicode characters with an empty string
                    result.Append("");
                }
            }
            result = result.Replace(Environment.NewLine, " ");
            result = result.Replace("&", "&amp;");
            result = result.Replace("<", "&lt;");
            result = result.Replace(">", "&gt;");
            result = result.Replace("'", "&apos;");
            result = result.Replace("\"", "&quot;");
            return result.ToString().Trim();
        }
        private bool IsUnicodeCharacter(char c)
        {
            if (char.IsControl(c))
            {
                return false; // Non-Unicode character found
            }
            return true;
        }
    }
}
