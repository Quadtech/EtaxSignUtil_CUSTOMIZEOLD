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
    public class PRLayout : BaseLayout
    {
        public FileControl FileControl { get; private set; }
        public RC_INV.DocumentHeader DocumentHeader { get; private set; }
        public BuyerInformation BuyerInformation { get; private set; }
        public List<RC_INV.TradeLineItemInformation> ListTradeLineItemInformation { get; private set; }
        public RC_INV.DocumentFooter DocumentFooter { get; private set; }
        public FileTrailer FileTrailer { get; private set; }
        public PRLayout(DBSimple dbSimple, QEB.Center QEBCenterInfo)
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
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowHeader);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowHeader, 0);
            string QueryHeader =
$@"SELECT H.PRNo AS ReceiptNo
,H.PRDate AS ReceiptDate
,H.PRNote AS FieldNote
,H.PayReceiveCode AS CustomerCode
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
,H.RemittedCurrencyCode AS CurrencyCode
,H.RemittedCurrency AS TotalAmountCurrencyAfterVAT
FROM CashBankPRHeader H
LEFT JOIN CustomerMaster CM
ON CM.Code = H.PayReceiveCode
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
WHERE H.DocumentTypeCode = '{DocumentTypeCode}'
AND H.RunNo = {RunNo}";
            DataTable TBHeader = new DataTable();
            TransErr = dbSimple.FillData(TBHeader, QueryHeader, ref ErrCode, ref ErrMsg);
            if (TransErr != 0 || TBHeader.Rows.Count == 0)
            {
                ErrMsg = "No Header Select";
                return ++TransErr;
            }

            string QueryDetails =
$@"SELECT D.DocNo AS TransactionDescription
, D.TotalToRemitCurrency AS TotalAmountAfterDiscount
FROM CashBankPRHeader H
INNER JOIN CashBankPRCB PRCB
ON H.DocumentTypeCode = PRCB.DocumentTypeCode
AND H.RunNo = PRCB.RunNo
INNER JOIN CashBankDoc D
ON D.DocumentTypeCode = PRCB.RefIssuedDocumentTypeCode
AND D.RunNo = PRCB.CBRunNo
WHERE H.DocumentTypeCode = '{DocumentTypeCode}'
AND H.RunNo = '{RunNo}'";
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
            string FieldNote = ReceiveValue.StringReceive("FieldNote", rowHeaderUpdate);

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
            double TotalAmountCurrencyAfterVAT = ReceiveValue.DoubleReceive("TotalAmountCurrencyAfterVAT", rowHeaderUpdate, 0);

            this.FileControl = new FileControl(this.SellerInformation.SELLER_TAX_ID, this.SellerInformation.SELLER_BRANCH_ID);

            this.DocumentHeader = new RC_INV.DocumentHeader(ReceiptNo
             , ReceiptDate
             , ""
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
            this.ListTradeLineItemInformation = new List<TradeLineItemInformation>();
            foreach (DataRow rowSIDetailsUpdate in TBDetails.Rows)
            {
                index++;
                string TransactionDescription = ReceiveValue.StringReceive("TransactionDescription", rowSIDetailsUpdate);
                double TotalAmountAfterDiscount = ReceiveValue.DoubleReceive("TotalAmountAfterDiscount", rowSIDetailsUpdate, 0);
                RC_INV.TradeLineItemInformation TradeLineItemInformation = new RC_INV.TradeLineItemInformation(index
                    , TransactionDescription
                    , 1
                    , "DOC"
                    , CurrencyCode
                    , TotalAmountAfterDiscount);
                this.ListTradeLineItemInformation.Add(TradeLineItemInformation);
            }
            this.DocumentFooter = new RC_INV.DocumentFooter(index, CurrencyCode, TotalAmountCurrencyAfterVAT);

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
            string tab = "\t";
            #region IncludedSupplyChainTradeLineItem
            #region IncludedSupplyChainTradeLineItem
            string AssociatedDocumentLineDocument =
$@"{tab}{tab}{tab}<ram:AssociatedDocumentLineDocument>
{tab}{tab}{tab}{tab}<ram:LineID>{item.LINE_ID}</ram:LineID>
{tab}{tab}{tab}</ram:AssociatedDocumentLineDocument>";
            #endregion
            #region SpecifiedTradeProduct
            string SpecifiedTradeProduct =
$@"{tab}{tab}{tab}<ram:SpecifiedTradeProduct>
{tab}{tab}{tab}{tab}<ram:Name>{item.PRODUCT_NAME}</ram:Name>
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
{tab}{tab}{tab}{tab}<ram:BilledQuantity unitCode=""{item.PRODUCT_UNIT_CODE}"">{item.PRODUCT_QUANTITY}</ram:BilledQuantity>
{tab}{tab}{tab}</ram:SpecifiedLineTradeDelivery>";
            #endregion
            #region SpecifiedLineTradeSettlement
            #region SpecifiedTradeAllowanceCharge
            string SpecifiedTradeAllowanceCharge = "";
            if (item.LINE_ALLOWANCE_CHARGE_IND == "true")
                SpecifiedTradeAllowanceCharge =
    $@"{tab}{tab}{tab}{tab}<ram:SpecifiedTradeAllowanceCharge>
{tab}{tab}{tab}{tab}{tab}<ram:ChargeIndicator>{item.LINE_ALLOWANCE_CHARGE_IND}</ram:ChargeIndicator>
{tab}{tab}{tab}{tab}{tab}<ram:ActualAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_ALLOWANCE_ACTUAL_AMOUNT}</ram:ActualAmount>
{tab}{tab}{tab}{tab}{tab}<ram:TypeCode>{item.LINE_ALLOWANCE_TYPE_CODE}</ram:TypeCode> 
{tab}{tab}{tab}{tab}</ram:SpecifiedTradeAllowanceCharge>";
            #endregion

            #region SpecifiedTradeSettlementLineMonetarySummation
            string SpecifiedTradeSettlementLineMonetarySummation =
$@"{tab}{tab}{tab}{tab}<ram:SpecifiedTradeSettlementLineMonetarySummation>
{tab}{tab}{tab}{tab}{tab}<ram:NetIncludingTaxesLineTotalAmount currencyID=""{item.LINE_BASIS_CURRENCY_CODE}"">{item.LINE_NET_INCLUDE_TAX_TOTAL_AMOUNT}</ram:NetIncludingTaxesLineTotalAmount>
{tab}{tab}{tab}{tab}</ram:SpecifiedTradeSettlementLineMonetarySummation>";
            #endregion
            string SpecifiedLineTradeSettlement =
$@"{tab}{tab}{tab}<ram:SpecifiedLineTradeSettlement>
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
{tab}{tab}{tab}<ram:ID schemeAgencyID=""ETDA"" schemeVersionID=""v2.0"">ER3-2560</ram:ID>
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
{tab}</rsm:ExchangedDocument>";
            #endregion
            #region SupplyChainTradeTransaction
            #region ApplicableHeaderTradeAgreement
            #region SellerTradeParty
            #region Seller_PostalTradeAddress
            string Seller_PostalTradeAddress = "";
            if (this.SellerInformation.SELLER_ADDRESS_LINE1 != "")
            {
                Seller_PostalTradeAddress =
$@"{tab}{tab}{tab}{tab}{tab}<ram:LineOne>{this.SellerInformation.SELLER_ADDRESS_LINE1}</ram:LineOne>
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_ADDRESS_LINE2 == "" ? "" : $"<ram:LineTwo>{this.SellerInformation.SELLER_ADDRESS_LINE2}</ram:LineTwo>")}
{tab}{tab}{tab}{tab}{tab}<ram:CountryID>{this.SellerInformation.SELLER_COUNTRY_ID}</ram:CountryID>";
            }
            else
            {
                Seller_PostalTradeAddress =
$@"{tab}{tab}{tab}{tab}{tab}<ram:PostcodeCode>{this.SellerInformation.SELLER_POST_CODE}</ram:PostcodeCode>
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_BUILDING_NAME == "" ? "" : $"<ram:BuildingName>{this.SellerInformation.SELLER_BUILDING_NAME}</ram:BuildingName>")}
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_ADDRESS_LINE3 == "" ? "" : $"<ram:LineThree>{this.SellerInformation.SELLER_ADDRESS_LINE3}</ram:LineThree>")}
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_ADDRESS_LINE4 == "" ? "" : $"<ram:LineFour>{this.SellerInformation.SELLER_ADDRESS_LINE4}</ram:LineFour>")}
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_ADDRESS_LINE5 == "" ? "" : $"<ram:LineFive>{this.SellerInformation.SELLER_ADDRESS_LINE5}</ram:LineFive>")}
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_STREET_NAME == "" ? "" : $"<ram:StreetName>{this.SellerInformation.SELLER_STREET_NAME}</ram:StreetName>")}
{tab}{tab}{tab}{tab}{tab}<ram:CityName>{this.SellerInformation.SELLER_CITY_ID}</ram:CityName>
{tab}{tab}{tab}{tab}{tab}<ram:CitySubDivisionName>{this.SellerInformation.SELLER_CITY_SUB_DIV_ID}</ram:CitySubDivisionName>
{tab}{tab}{tab}{tab}{tab}<ram:CountryID>{this.SellerInformation.SELLER_COUNTRY_ID}</ram:CountryID>
{tab}{tab}{tab}{tab}{tab}<ram:CountrySubDivisionID>{this.SellerInformation.SELLER_COUNTRY_SUB_DIV_ID}</ram:CountrySubDivisionID>
{tab}{tab}{tab}{tab}{tab}{(this.SellerInformation.SELLER_BUILDING_NO == "" ? "" : $"<ram:BuildingNumber>{this.SellerInformation.SELLER_BUILDING_NO}</ram:BuildingNumber>")}";
            }
            #endregion
            string SellerTradeParty =
$@"{tab}{tab}{tab}<ram:SellerTradeParty>
{tab}{tab}{tab}{tab}<ram:Name>{this.SellerInformation.SELLER_NAME}</ram:Name>
{tab}{tab}{tab}{tab}<ram:SpecifiedTaxRegistration>
{tab}{tab}{tab}{tab}{tab}<ram:ID schemeID=""TXID"">{this.SellerInformation.SELLER_TAX_ID + this.SellerInformation.SELLER_BRANCH_ID}</ram:ID>
{tab}{tab}{tab}{tab}</ram:SpecifiedTaxRegistration>
{tab}{tab}{tab}{tab}<ram:PostalTradeAddress>
{Seller_PostalTradeAddress}
{tab}{tab}{tab}{tab}</ram:PostalTradeAddress>
{tab}{tab}{tab}</ram:SellerTradeParty>";
            #endregion
            #region BuyerTradeParty
            #region DefinedTradeContact
            string DefinedTradeContact = "";
            // ตัดช่องว่างหัวท้ายก่อนเช็ค กันอีเมลลูกค้าที่มีแต่ช่องว่างทำให้ tag URIID ว่าง (schema กรมสรรพากรบังคับอย่างน้อย 1 ตัวอักษร)
            string BuyerURIID = (BuyerInformation.BUYER_URIID ?? "").Trim();
            if (BuyerURIID != "")
            {
                DefinedTradeContact =
$@"{tab}{tab}{tab}{tab}<ram:DefinedTradeContact>
{tab}{tab}{tab}{tab}{tab}<ram:EmailURIUniversalCommunication>
{tab}{tab}{tab}{tab}{tab}{tab}<ram:URIID>{BuyerURIID}</ram:URIID>
{tab}{tab}{tab}{tab}{tab}</ram:EmailURIUniversalCommunication>
{tab}{tab}{tab}{tab}</ram:DefinedTradeContact>";
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
$@"{tab}{tab}{tab}<ram:BuyerTradeParty>
{tab}{tab}{tab}{tab}<ram:ID>{this.BuyerInformation.BUYER_ID}</ram:ID>
{tab}{tab}{tab}{tab}<ram:Name>{this.BuyerInformation.BUYER_NAME}</ram:Name>
{tab}{tab}{tab}{tab}<ram:SpecifiedTaxRegistration>
{tab}{tab}{tab}{tab}{tab}<ram:ID schemeID=""{this.BuyerInformation.BUYER_TAX_ID_TYPE}"">{this.BuyerInformation.BUYER_TAX_ID + this.BuyerInformation.BUYER_BRANCH_ID}</ram:ID>
{tab}{tab}{tab}{tab}</ram:SpecifiedTaxRegistration>
{DefinedTradeContact}
{tab}{tab}{tab}{tab}<ram:PostalTradeAddress>
{Buyer_PostalTradeAddress}
{tab}{tab}{tab}{tab}</ram:PostalTradeAddress>
{tab}{tab}{tab}</ram:BuyerTradeParty>";
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
$@"{tab}{tab}<ram:ApplicableHeaderTradeDelivery>
{tab}{tab}{tab}<ram:ShipFromTradeParty>
{tab}{tab}{tab}{tab}<ram:Name>{this.BuyerInformation.BUYER_NAME}</ram:Name>
{tab}{tab}{tab}</ram:ShipFromTradeParty>
{tab}{tab}</ram:ApplicableHeaderTradeDelivery>";
            #endregion
            #region ApplicableHeaderTradeSettlement
            #region InvoiceCurrencyCode
            string InvoiceCurrencyCode =
$@"{tab}{tab}{tab}<ram:InvoiceCurrencyCode>{DocumentFooter.INVOICE_CURRENCY_CODE}</ram:InvoiceCurrencyCode>";
            #endregion
            #region ApplicableTradeTax
            //            string ApplicableTradeTax =
            //$@"{tab}{tab}{tab}<ram:ApplicableTradeTax>
            //{tab}{tab}{tab}{tab}<ram:TypeCode>{DocumentFooter.TAX_TYPE_CODE}</ram:TypeCode>
            //{tab}{tab}{tab}{tab}<ram:CalculatedRate>{DocumentFooter.TAX_CAL_RATE}</ram:CalculatedRate>
            //{tab}{tab}{tab}{tab}<ram:BasisAmount currencyID=""{DocumentFooter.INVOICE_CURRENCY_CODE}"">{DocumentFooter.BASIS_AMOUNT}</ram:BasisAmount>
            //{tab}{tab}{tab}{tab}<ram:CalculatedAmount currencyID=""{DocumentFooter.INVOICE_CURRENCY_CODE}"">{DocumentFooter.TAX_CAL_AMOUNT}</ram:CalculatedAmount>
            //{tab}{tab}{tab}</ram:ApplicableTradeTax>";
            #endregion
            #region SpecifiedTradeAllowanceCharge
            //            string SpecifiedTradeAllowanceCharge =
            //$@"{tab}{tab}{tab}<ram:SpecifiedTradeAllowanceCharge>
            //{tab}{tab}{tab}{tab}<ram:ChargeIndicator>{DocumentFooter.ALLOWANCE_CHARGE_IND}</ram:ChargeIndicator>
            //{tab}{tab}{tab}{tab}<ram:ActualAmount currencyID=""{DocumentFooter.ALLOWANCE_TOTAL_CURRENCY_CODE}"">{DocumentFooter.ALLOWANCE_ACTUAL_AMOUNT}</ram:ActualAmount>
            //{tab}{tab}{tab}{tab}<ram:TypeCode>95</ram:TypeCode> 
            //{tab}{tab}{tab}</ram:SpecifiedTradeAllowanceCharge>";
            #endregion
            #region SpecifiedTradeSettlementHeaderMonetarySummation
            string SpecifiedTradeSettlementHeaderMonetarySummation =
$@"{tab}{tab}{tab}<ram:SpecifiedTradeSettlementHeaderMonetarySummation>
{tab}{tab}{tab}{tab}<ram:GrandTotalAmount currencyID=""{DocumentFooter.TAX_TOTAL_CURRENCY_CODE}"">{DocumentFooter.GRAND_TOTAL_AMOUNT}</ram:GrandTotalAmount>
{tab}{tab}{tab}</ram:SpecifiedTradeSettlementHeaderMonetarySummation>";
            #endregion
            string ApplicableHeaderTradeSettlement =
$@"{tab}{tab}<ram:ApplicableHeaderTradeSettlement>
{InvoiceCurrencyCode}
{SpecifiedTradeSettlementHeaderMonetarySummation}
{tab}{tab}</ram:ApplicableHeaderTradeSettlement>";
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
<?xml-model href=""Receipt_Schematron_2p0.sch"" type=""application/xml"" schematypens=""http://purl.oclc.org/dsdl/schematron""?>
<rsm:Receipt_CrossIndustryInvoice xmlns:rsm=""urn:etda:uncefact:data:standard:Receipt_CrossIndustryInvoice:2"" xmlns:ram=""urn:etda:uncefact:data:standard:Receipt_ReusableAggregateBusinessInformationEntity:2"">
{ExchangedDocumentContext}
{ExchangedDocument}
{SupplyChainTradeTransaction}
</rsm:Receipt_CrossIndustryInvoice>";
            return result;
        }
    }
}
