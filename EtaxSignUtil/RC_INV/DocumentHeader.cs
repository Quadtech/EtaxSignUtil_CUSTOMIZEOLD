using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EtaxSignUtil.RC_INV
{
    public class DocumentHeader : EtaxSignUtil.Layout.DocumentHeader
    {
        public DocumentHeader(string DOCUMENT_ID
            , DateTime DOCUMENT_ISSUE_DTM
            , string BUYER_ORDER_ASSIGN_ID
            , string DOCUMENT_REMARK)
            : base("ใบรับ (ใบเสร็จรับเงิน)"
            , "T01"
            , DOCUMENT_ID
            , DOCUMENT_ISSUE_DTM
            , BUYER_ORDER_ASSIGN_ID
            , DOCUMENT_REMARK)
        {

        }
    }
}
