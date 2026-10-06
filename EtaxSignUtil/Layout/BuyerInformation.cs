using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using DBUtil;
using System.Data;
using ModuleUtil;
using System.Reflection;
using System.ComponentModel;

namespace EtaxSignUtil.Layout
{
    public class BuyerInformation : TaxFileLayout
    {
        [Required()]
        [StringLength(1)]
        [Description("ประเภทรายการ[DATA_TYPE]")]
        public virtual string DATA_TYPE { get; set; }


        [StringLength(35)]
        [Description("รหัสผู้ซื้อ[BUYER_ID]")]
        public virtual string BUYER_ID { get; set; }


        [Required()]
        [StringLength(256)]
        [Description("ชื่อผู้ซื้อ[BUYER_NAME]")]
        public virtual string BUYER_NAME { get; set; }


        [Required()]
        [StringLength(4)]
        [Description("ประเภทผู้เสียภาษีอากร [BUYER_TAX_ID_TYPE]")]
        public virtual string BUYER_TAX_ID_TYPE { get; set; }


        [Required()]
        [StringLength(35)]
        [Description("เลขประจำตัวผู้เสียภาษีอากร[BUYER_TAX_ID]")]
        public virtual string BUYER_TAX_ID { get; set; }


        [StringLength(5)]
        [Description("เลขที่สาขา[BUYER_BRANCH_ID]")]
        public virtual string BUYER_BRANCH_ID { get; set; }


        [StringLength(140)]
        [Description("ชื่อผู้ติดต่อ[BUYER_CONTACT_PERSON_NAME]")]
        public virtual string BUYER_CONTACT_PERSON_NAME { get; set; }

        [StringLength(140)]
        [Description("ชื่อแผนก[BUYER_CONTACT_DEPARTMENT_NAME]")]
        public virtual string BUYER_CONTACT_DEPARTMENT_NAME { get; set; }

        [StringLength(256)]
        [Description("อีเมล[BUYER_URIID]")]
        public virtual string BUYER_URIID { get; set; }


        [StringLength(35)]
        [Description("เบอร์โทรศัพท์[BUYER_PHONE_NO]")]
        public virtual string BUYER_PHONE_NO { get; set; }


        [StringLength(16)]
        [Description("รหัสไปรษณีย์[BUYER_POST_CODE]")]
        public virtual string BUYER_POST_CODE { get; set; }


        [StringLength(70)]
        [Description("ชื่ออาคาร[BUYER_BUILDING_NAME]")]
        public virtual string BUYER_BUILDING_NAME { get; set; }


        [StringLength(16)]
        [Description("บ้านเลขที่[BUYER_BUILDING_NO]")]
        public virtual string BUYER_BUILDING_NO { get; set; }


        [StringLength(256)]
        [Description("ที่อยู่บรรทัดที่ 1[BUYER_ADDRESS_LINE1]")]
        public virtual string BUYER_ADDRESS_LINE1 { get; set; }


        [StringLength(256)]
        [Description("ที่อยู่บรรทัดที่ 2[BUYER_ADDRESS_LINE2]")]
        public virtual string BUYER_ADDRESS_LINE2 { get; set; }


        [StringLength(70)]
        [Description("ซอย[BUYER_ADDRESS_LINE3]")]
        public virtual string BUYER_ADDRESS_LINE3 { get; set; }


        [StringLength(70)]
        [Description("หมู่บ้าน[BUYER_ADDRESS_LINE4]")]
        public virtual string BUYER_ADDRESS_LINE4 { get; set; }


        [StringLength(70)]
        [Description("หมู่ที่[BUYER_ADDRESS_LINE5]")]
        public virtual string BUYER_ADDRESS_LINE5 { get; set; }


        [StringLength(70)]
        [Description("ถนน[BUYER_STREET_NAME]")]
        public virtual string BUYER_STREET_NAME { get; set; }


        [StringLength(70)]
        [Description("รหัสตำบล[BUYER_CITY_SUB_DIV_ID]")]
        public virtual string BUYER_CITY_SUB_DIV_ID { get; set; }


        [StringLength(70)]
        [Description("ชื่อตำบล[BUYER_CITY_SUB_DIV_NAME]")]
        public virtual string BUYER_CITY_SUB_DIV_NAME { get; set; }


        [StringLength(70)]
        [Description("รหัสอำเภอ[BUYER_CITY_ID]")]
        public virtual string BUYER_CITY_ID { get; set; }


        [StringLength(70)]
        [Description("ชื่ออำเภอ[BUYER_CITY_NAME]")]
        public virtual string BUYER_CITY_NAME { get; set; }


        [StringLength(70)]
        [Description("รหัสจังหวัด[BUYER_COUNTRY_SUB_DIV_ID]")]
        public virtual string BUYER_COUNTRY_SUB_DIV_ID { get; set; }


        [StringLength(70)]
        [Description("ชื่อจังหวัด[BUYER_COUNTRY_SUB_DIV_NAME]")]
        public virtual string BUYER_COUNTRY_SUB_DIV_NAME { get; set; }


        [Required()]
        [StringLength(3)]
        [Description("รหัสประเทศ[BUYER_COUNTRY_ID]")]
        public virtual string BUYER_COUNTRY_ID { get; set; }

        public BuyerInformation(string BUYER_ID
            , string BUYER_NAME
            , string BUYER_PERSONALITY_TYPE
            , string BUYER_TAX_ID
            , string BUYER_HEADQUATER_OR_BRANCH
            , string BUYER_BRANCH_ID
            , string BUYER_URIID
            , string BUYER_PHONE_NO
            , string BUYER_ADDRESS
            , int BUYER_KWAENGTAMBON_CODE
            , string BUYER_KWAENGTAMBON_NAME
            , int BUYER_AMPORCITY_CODE
            , string BUYER_AMPORCITY_NAME
            , int BUYER_PROVINCE_CODE
            , string BUYER_PROVINCE_NAME
            , bool BUYER_KWAENGKHET_TRUEFALSE
            , string BUYER_COUNTRY_ID
            , string BUYER_POST_CODE
            , string BUYER_KWAENGTAMBON_ETAX
            , string BUYER_AMPORCITY_CODE_ETAX
            , string BUYER_PROVINCE_CODE_ETAX
            , bool IsDomestic)
        {
            BUYER_HEADQUATER_OR_BRANCH = BUYER_HEADQUATER_OR_BRANCH.Trim();
            BUYER_BRANCH_ID = BUYER_BRANCH_ID.Trim();

            string BUYER_TAX_ID_TYPE = "TXID";
            if (!IsDomestic)
            {
                BUYER_TAX_ID_TYPE = "OTHR";
                BUYER_TAX_ID = "N/A";
                BUYER_BRANCH_ID = "";
            }
            else if (BUYER_PERSONALITY_TYPE == "C")
            {
                BUYER_TAX_ID_TYPE = "TXID";
                if (BUYER_HEADQUATER_OR_BRANCH == "H" || BUYER_HEADQUATER_OR_BRANCH == "" || BUYER_BRANCH_ID == "")
                    BUYER_BRANCH_ID = "00000";
            }
            else if (BUYER_PERSONALITY_TYPE == "P")
            {
                BUYER_TAX_ID_TYPE = "NIDN";
                if (String.IsNullOrEmpty(BUYER_TAX_ID) || BUYER_TAX_ID == "0000000000000")
                {
                    BUYER_TAX_ID_TYPE = "OTHR";
                    BUYER_TAX_ID = "N/A";
                    BUYER_BRANCH_ID = "";
                }
            }
            else
            {
                BUYER_TAX_ID_TYPE = "OTHR";
                if (String.IsNullOrEmpty(BUYER_TAX_ID) || BUYER_TAX_ID == "0000000000000")
                {
                    BUYER_TAX_ID = "N/A";
                    BUYER_BRANCH_ID = "";
                }
                else
                {
                    BUYER_TAX_ID_TYPE = "TXID";
                    if (BUYER_HEADQUATER_OR_BRANCH == "H" || BUYER_HEADQUATER_OR_BRANCH == "" || BUYER_BRANCH_ID == "")
                        BUYER_BRANCH_ID = "00000";
                }
            }

            string BUILDING_NO = "";
            string BUILDING_NAME = "";
            string ADDRESS_LINE1 = "";
            string ADDRESS_LINE2 = "";
            string ADDRESS_LINE3 = "";
            string ADDRESS_LINE4 = "";
            string ADDRESS_LINE5 = "";
            string STREET_NAME = "";
            string CITY_SUB_DIV_ID = "";
            string CITY_SUB_DIV_NAME = "";
            string CITY_ID = "";
            string CITY_NAME = "";
            string COUNTRY_SUB_DIV_ID = "";
            string COUNTRY_SUB_DIV_NAME = "";

            BUYER_COUNTRY_ID = BUYER_COUNTRY_ID.ToUpper();
            if (BUYER_COUNTRY_ID == "")
                BUYER_COUNTRY_ID = "TH";
            if (BUYER_COUNTRY_ID != "TH")
            {
                ADDRESS_LINE1 = BUYER_ADDRESS;
                BUYER_KWAENGTAMBON_NAME = "";
                BUYER_AMPORCITY_NAME = "";
                BUYER_PROVINCE_NAME = "";
                BUYER_TAX_ID_TYPE = "OTHR";
                BUYER_TAX_ID = "N/A";
                BUYER_BRANCH_ID = "";
            }
            else
            {
                if (BUYER_PROVINCE_CODE > 0 && BUYER_AMPORCITY_CODE > 0 && BUYER_KWAENGTAMBON_CODE > 0)
                {
                    BUILDING_NO = this.Get_BUILDING_NO(BUYER_ADDRESS);
                    BUILDING_NAME = this.Get_BUILDING_NAME(BUYER_ADDRESS);
                    ADDRESS_LINE3 = this.Get_ADDRESS_LINE3(BUYER_ADDRESS);
                    ADDRESS_LINE4 = this.Get_ADDRESS_LINE4(BUYER_ADDRESS);
                    ADDRESS_LINE5 = this.Get_ADDRESS_LINE5(BUYER_ADDRESS);
                    STREET_NAME = this.Get_STREET_NAME(BUYER_ADDRESS);

                    if (String.IsNullOrEmpty(BUYER_PROVINCE_CODE_ETAX))
                        COUNTRY_SUB_DIV_ID = BUYER_PROVINCE_CODE.ToString("00");
                    else
                        COUNTRY_SUB_DIV_ID = BUYER_PROVINCE_CODE_ETAX;
                    COUNTRY_SUB_DIV_NAME = BUYER_PROVINCE_NAME;

                    if (String.IsNullOrEmpty(BUYER_AMPORCITY_CODE_ETAX))
                        CITY_ID = COUNTRY_SUB_DIV_ID + BUYER_AMPORCITY_CODE.ToString("00");
                    else
                        CITY_ID = BUYER_AMPORCITY_CODE_ETAX;
                    CITY_NAME = BUYER_AMPORCITY_NAME;

                    if (String.IsNullOrEmpty(BUYER_KWAENGTAMBON_ETAX))
                        CITY_SUB_DIV_ID = CITY_ID + BUYER_KWAENGTAMBON_CODE.ToString("00");
                    else
                        CITY_SUB_DIV_ID = BUYER_KWAENGTAMBON_ETAX;
                    CITY_SUB_DIV_NAME = BUYER_KWAENGTAMBON_NAME;
                }
                else
                {
                    ADDRESS_LINE1 = BUYER_ADDRESS;

                    if (BUYER_KWAENGTAMBON_NAME != "")
                        ADDRESS_LINE2 = String.Format(BUYER_KWAENGKHET_TRUEFALSE ? "แขวง{0}" : "ต.{0}", BUYER_KWAENGTAMBON_NAME);
                    if (BUYER_AMPORCITY_NAME != "")
                    {
                        if (ADDRESS_LINE2 != "")
                            ADDRESS_LINE2 += " ";
                        ADDRESS_LINE2 += String.Format(BUYER_KWAENGKHET_TRUEFALSE ? "เขต{0}" : "อ.{0}", BUYER_AMPORCITY_NAME);
                    }
                    if (BUYER_PROVINCE_NAME != "")
                    {
                        if (ADDRESS_LINE2 != "")
                            ADDRESS_LINE2 += " ";
                        ADDRESS_LINE2 += String.Format(BUYER_KWAENGKHET_TRUEFALSE ? "{0}" : "จ.{0}", BUYER_PROVINCE_NAME);
                    }

                    BUILDING_NO = this.Get_BUILDING_NO(BUYER_ADDRESS);
                    if (BUYER_PROVINCE_NAME != "")
                    {
                        if (String.IsNullOrEmpty(BUYER_PROVINCE_CODE_ETAX))
                            COUNTRY_SUB_DIV_ID = BUYER_PROVINCE_CODE.ToString("00");
                        else
                            COUNTRY_SUB_DIV_ID = BUYER_PROVINCE_CODE_ETAX;
                        COUNTRY_SUB_DIV_NAME = BUYER_PROVINCE_NAME;
                    }
                    if (BUYER_AMPORCITY_NAME != "")
                    {
                        if (String.IsNullOrEmpty(BUYER_AMPORCITY_CODE_ETAX))
                            CITY_ID = COUNTRY_SUB_DIV_ID + BUYER_AMPORCITY_CODE.ToString("00");
                        else
                            CITY_ID = BUYER_AMPORCITY_CODE_ETAX;
                        CITY_NAME = BUYER_AMPORCITY_NAME;
                    }
                    if (BUYER_KWAENGTAMBON_NAME != "")
                    {
                        if (String.IsNullOrEmpty(BUYER_KWAENGTAMBON_ETAX))
                            CITY_SUB_DIV_ID = CITY_ID + BUYER_KWAENGTAMBON_CODE.ToString("00");
                        else
                            CITY_SUB_DIV_ID = BUYER_KWAENGTAMBON_ETAX;
                        CITY_SUB_DIV_NAME = BUYER_KWAENGTAMBON_NAME;
                    }
                }
            }

            this.DATA_TYPE = "B";
            this.BUYER_ID = BUYER_ID;
            this.BUYER_NAME = BUYER_NAME.Replace("&", "And");
            this.BUYER_TAX_ID_TYPE = BUYER_TAX_ID_TYPE;
            this.BUYER_TAX_ID = BUYER_TAX_ID;
            this.BUYER_BRANCH_ID = BUYER_BRANCH_ID;
            this.BUYER_URIID = BUYER_URIID;
            this.BUYER_PHONE_NO = BUYER_PHONE_NO;
            this.BUYER_POST_CODE = BUYER_POST_CODE;
            this.BUYER_BUILDING_NO = GetStringXML(BUILDING_NO, false);
            if (String.IsNullOrEmpty(this.BUYER_BUILDING_NO))
            {
                ADDRESS_LINE1 = GetStringXML(BUYER_ADDRESS, false);
            }
            else
            {
                this.BUYER_BUILDING_NAME = GetStringXML(BUILDING_NAME, false);
                this.BUYER_ADDRESS_LINE3 = GetStringXML(ADDRESS_LINE3, false);
                this.BUYER_ADDRESS_LINE4 = GetStringXML(ADDRESS_LINE4, false);
                this.BUYER_ADDRESS_LINE5 = GetStringXML(ADDRESS_LINE5, false);
                this.BUYER_STREET_NAME = GetStringXML(STREET_NAME, false);
            }
            this.BUYER_ADDRESS_LINE1 = GetStringXML(ADDRESS_LINE1, false);
            this.BUYER_ADDRESS_LINE2 = GetStringXML(ADDRESS_LINE2, false);
            this.BUYER_CITY_SUB_DIV_ID = GetStringXML(CITY_SUB_DIV_ID, false);
            this.BUYER_CITY_SUB_DIV_NAME = GetStringXML(CITY_SUB_DIV_NAME, false);
            this.BUYER_CITY_ID = GetStringXML(CITY_ID, false);
            this.BUYER_CITY_NAME = GetStringXML(CITY_NAME, false);
            this.BUYER_COUNTRY_SUB_DIV_ID = GetStringXML(COUNTRY_SUB_DIV_ID, false);
            this.BUYER_COUNTRY_SUB_DIV_NAME = GetStringXML(COUNTRY_SUB_DIV_NAME, false);
            this.BUYER_COUNTRY_ID = GetStringXML(BUYER_COUNTRY_ID, false);
        }

        /// <summary>
        /// ตรวจข้อมูลผู้ซื้อก่อนสร้างไฟล์ e-tax (เลขผู้เสียภาษี/สาขา/รหัสไปรษณีย์/ที่อยู่/อีเมล)
        /// ไม่ผ่านคืนค่ามากกว่า 0 พร้อมข้อความใน ErrMsg
        /// </summary>
        public int ValidateInfo(ref string ErrMsg)
        {
            int TransErr = 0;
            if ((this.BUYER_TAX_ID_TYPE == "TXID" || this.BUYER_TAX_ID_TYPE == "NIDN")
              && this.BUYER_TAX_ID.Length != 13)
            {
                ErrMsg = $"เลขประจำตัวผู้เสียภาษีอากร ไม่ครบ 13 หลัก : {this.BUYER_TAX_ID}";
                return ++TransErr;
            }
            if (this.BUYER_TAX_ID_TYPE == "TXID" && this.BUYER_BRANCH_ID.Length != 5)
            {
                ErrMsg = $"รหัสสาขา ไม่ครบ 5 หลัก : {this.BUYER_BRANCH_ID}";
                return ++TransErr;
            }
            if (this.BUYER_COUNTRY_ID == "TH" && this.BUYER_POST_CODE == "")
            {
                ErrMsg = "กรุณาระบุ รหัสไปรษณีย์ ที่โปรแกรมข้อมูลลูกค้า";
                return ++TransErr;
            }
            if (this.BUYER_COUNTRY_ID == "TH" && this.BUYER_POST_CODE.Length != 5)
            {
                ErrMsg = "ต้องระบุรหัสไปรษณีย์ผู้ซื้อ จำนวนตัวเลขเท่ากับ 5 หลัก";
                return ++TransErr;
            }
            if (this.BUYER_ADDRESS_LINE1 == "")
            {
                if (this.BUYER_BUILDING_NO == "")
                {
                    ErrMsg = "บ้านเลขที่ไม่ถูกต้องตามฟอร์แมต";
                    return ++TransErr;
                }
                if (this.BUYER_COUNTRY_SUB_DIV_ID == "")
                {
                    ErrMsg = "กรุณาระบุ รหัสจังหวัด ที่โปรแกรมข้อมูลลูกค้า";
                    return ++TransErr;
                }
                if (this.BUYER_CITY_SUB_DIV_ID == "")
                {
                    ErrMsg = "กรุณาระบุ รหัสอำเภอ/เขต ที่โปรแกรมข้อมูลลูกค้า";
                    return ++TransErr;
                }
                if (this.BUYER_CITY_ID == "")
                {
                    ErrMsg = "กรุณาระบุ รหัสตำบล/แขวงที่โปรแกรมข้อมูลลูกค้า";
                    return ++TransErr;
                }
            }
            // อีเมลที่มี < > (เช่น 'ชื่อ' <a@b.com>) ไม่ใช่อีเมลที่ถูกต้อง และทำให้ไฟล์ XML พัง sign ไม่ผ่าน ต้องแก้ที่ข้อมูลลูกค้า
            string BuyerURIID = (this.BUYER_URIID ?? "").Trim();
            if (BuyerURIID.IndexOfAny(new char[] { '<', '>' }) >= 0)
            {
                ErrMsg = $@"อีเมลลูกค้าไม่ถูกต้อง : {BuyerURIID}
กรุณาแก้ที่โปรแกรมข้อมูลลูกค้า ให้เหลือแค่อีเมล ห้ามมีเครื่องหมาย < >";
                return ++TransErr;
            }
            return TransErr;
        }
    }
}
