using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ModuleUtil;
using DevExpress.XtraReports.UI;
using System.IO;
using QEB;
using DBUtil;
using System.Data.OleDb;
using System.Diagnostics;
using EtaxSignUtil.SI;
using UtilityHelper.Print_Document;
using ControlCenter;
using DevExpress.XtraEditors;
using System.Data.SqlClient;
using MessageUtil;

namespace EtaxSignUtil
{
    public partial class MultiSignETAXForm : System.Windows.Forms.Form
    {
        private const int CommandTimeout = 300;
        private Center QEBCenterInfo { get; set; }
        private DBSimple DBSimpleObj { get; set; }
        private string SignCompanyName { get; set; } = "";
        private DelegatePrintPreview CallbackPrintPreview { get; set; }
        private DataSet DataSetObj { get; set; }
        private XtraReport ReportObj { get; set; }
        private string ModuleCode { get; set; }
        private string DocumentTypeCode { get; set; }
        private string FIELD_DocNo { get; set; }
        private string TableNameDetails { get; set; }
        private UtilityHelper.PrintUtil.EnumPrint PrintInsertRow { get; set; }
        private DataTable TBEtax { get; set; }
        private DataTable TBSIHeader { get; set; }
        private DataTable TBCNDNSalesHeader { get; set; }
        private DataTable TBCBPRHeader { get; set; }
        private DataTable TBRCHeader { get; set; }
        private bool UseEtax { get; set; } = true;
        private EtaxMail EtaxMail { get; set; }
        private TokenInfo Token { get; set; }
        private bool UsePdfA3 { get; set; } = false;
        public MultiSignETAXForm(Center QEBCenterInfo
            , DBSimple DBSimpleObj
            , DelegatePrintPreview CallbackPrintPreview
            , DataSet DataSetObj
            , XtraReport ReportObj
            , string ModuleCode
            , string DocumentTypeCode
            , string TableNameDetails
            , string FIELD_DocNo
            , UtilityHelper.PrintUtil.EnumPrint PrintInsertRow)
        {
            InitializeComponent();
            this.QEBCenterInfo = QEBCenterInfo;
            this.DBSimpleObj = DBSimpleObj;
            this.CallbackPrintPreview = CallbackPrintPreview;
            this.DataSetObj = DataSetObj;
            this.ReportObj = ReportObj;
            this.ModuleCode = ModuleCode;
            this.DocumentTypeCode = DocumentTypeCode;
            this.FIELD_DocNo = FIELD_DocNo;
            this.TableNameDetails = TableNameDetails;
            this.PrintInsertRow = PrintInsertRow;
            this.InitTable();
            this.InitControl();
            this.LoadCompany();
            ResourceLoader.DeleteFileTempPath();
        }
        public DialogResult ShowPopup()
        {
            this.LoadData();
            return this.ShowDialog();
        }
        private void InitTable()
        {
            this.TBEtax = new DataTable();
            this.TBEtax.Columns.Add("Select", typeof(bool));
            this.TBEtax.Columns.Add("DocumentModuleCode");
            this.TBEtax.Columns.Add("DocumentTypeCde");
            this.TBEtax.Columns.Add("RunNo", typeof(long));
            this.TBEtax.Columns.Add("TransactionDate", typeof(DateTime));
            this.TBEtax.Columns.Add("DocNo");
            this.TBEtax.Columns.Add("CustomerCode");
            this.TBEtax.Columns.Add("CustomerFullName");
            this.TBEtax.Columns.Add("IsSendMail", typeof(bool));

            this.TBSIHeader = new DataTable("SalesInvoiceHeader");
            this.DBSimpleObj.CreateDataTable(this.TBSIHeader, "SELECT * FROM SalesInvoiceHeader");
            this.TBCNDNSalesHeader = new DataTable("CreditDebitNoteSalesInvoiceHeader");
            this.DBSimpleObj.CreateDataTable(this.TBCNDNSalesHeader, "SELECT * FROM CreditDebitNoteSalesInvoiceHeader");
            this.TBCBPRHeader = new DataTable("CashBankPRHeader");
            this.DBSimpleObj.CreateDataTable(this.TBCBPRHeader, "SELECT * FROM CashBankPRHeader");
            this.TBRCHeader = new DataTable("ReceiptHeader");
            this.DBSimpleObj.CreateDataTable(this.TBRCHeader, "SELECT * FROM ReceiptHeader");

            DataTable TBConfig = new DataTable();
            string Query = $@"SELECT C.Value
FROM QERP.dbo.CompanyConfig C
WHERE C.CompanyCode = DB_NAME()
AND C.ConfigCode = 'UseEtaxPDFA3'
AND C.Value = 'Y'";
            this.DBSimpleObj.FillData(TBConfig, Query);
            if (TBConfig.Rows.Count > 0)
            {
                this.UsePdfA3 = true;
            }
        }
        private void InitControl()
        {
            this.Token = new TokenInfo();

            this.gridControl.DataSource = new DataView(this.TBEtax, "", "", DataViewRowState.CurrentRows);

            DataTable TBSearch = new DataTable();
            TBSearch.Columns.Add("Code");
            TBSearch.Columns.Add("Desc");
            TBSearch.Rows.Add("Date", "วันที่");
            TBSearch.Rows.Add("DocNo", "เลขที่");
            UtilityClass.SetupLookupEdit(TBSearch.DefaultView, "Code", "Desc", this.LookUpEdit_Search);
            this.LookUpEdit_Search.EditValue = "Date";
            this.LookUpEdit_Search.EditValueChanged += LookUpEdit_Search_EditValueChanged;

            DataTable TBCustomerType = TBSearch.Clone();
            TBCustomerType.Rows.Add("ALL", "ทั้งหมด");
            TBCustomerType.Rows.Add("C", "นิติบุคคล");
            TBCustomerType.Rows.Add("P", "บุคคลธรรมดา");
            UtilityClass.SetupLookupEdit(TBCustomerType.DefaultView, "Code", "Desc", this.LookUpEdit_CustomerType);
            this.LookUpEdit_CustomerType.EditValue = "ALL";
            this.LookUpEdit_CustomerType.EditValueChanged += LookUpEdit_CustomerType_EditValueChanged;

            this.DateEdit_From.EditValue = DateTime.Now.Date;
            this.DateEdit_To.EditValue = DateTime.Now.Date;

            this.repositoryItemButtonDetails.ButtonPressed += RepositoryItemButtonDetails_ButtonPressed;

            this.EnableSearchDate(true);
            string username = this.QEBCenterInfo.Username.ToLower();
            if (username == "sa" || username == "testuser")
                barButtonItemTest.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;

            this.EtaxMail = new EtaxMail(this.QEBCenterInfo, this.DBSimpleObj);
            this.EtaxMail.InitData();
            if (!this.EtaxMail.UseEtaxSendmail)
                this.gridColumnIsSendMail.Visible = false;


            if (this.UsePdfA3)
            {
                this.ButtonSettingToken.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                this.btnSignPDF.Text = "PDF/A-3";
                this.btnSignPDF.Click += BtnSignPDF_Click;
                this.barButtonItemTest.ItemClick += BarButtonItemTest_ItemClick;
            }
            else
            {
                this.btnSignPDF.Click += this.btnSignETAX_Click;
                this.barButtonItemTest.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonItemTest_ItemClick);
            }
        }
        private void EnableSearchDate(bool flag)
        {
            this.DateEdit_From.Enabled = flag;
            this.DateEdit_To.Enabled = flag;
            this.TextEdit_From.Enabled = !flag;
            this.TextEdit_To.Enabled = !flag;
        }
        public string GetQueryText(DateTime dateTime)
        {
            return $"{dateTime.Month}/{dateTime.Day}/{dateTime.Year}";
        }
        private void LookUpEdit_Search_EditValueChanged(object sender, EventArgs e)
        {
            if (this.LookUpEdit_Search.EditValue.ToString() == "Date")
                this.EnableSearchDate(true);
            else
                this.EnableSearchDate(false);
        }
        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }
        private void LoadData()
        {
            this.TBEtax.Clear();
            string CustomerType = this.LookUpEdit_CustomerType.EditValue.ToString();
            string Query = "";
            string FilterCustomerType = "";
            if (CustomerType != "ALL")
                FilterCustomerType += $"AND ISNULL(CM.PersonalityType,'C') = '{CustomerType}'";
            if (ModuleCode.StartsWith("SI"))
            {
                string QuerySearch = "";
                if (this.LookUpEdit_Search.EditValue.ToString() == "Date")
                    QuerySearch = this.GetConditionDate("H.InvoiceDate", this.DateEdit_From, this.DateEdit_To);
                else
                    QuerySearch = this.GetCondition("H.InvoiceNo", this.TextEdit_From, this.TextEdit_To);
                Query = $@"SELECT CAST(0 as bit) 'Select'
, H.DocumentModuleCode
, H.DocumentTypeCode
, H.RunNo
, H.InvoiceDate AS TransactionDate
, H.InvoiceNo AS DocNo
, H.CustomerCode
, LTRIM(RTRIM(ISNULL(H.CustomerPrefix,'') +' '+ ISNULL(H.CustomerName,'') + ' '+ISNULL(H.CustomerSuffix,''))) AS CustomerFullName
, CAST(1 as bit) AS IsSendMail
FROM SalesInvoiceHeader H
LEFT JOIN CustomerMaster CM
ON CM.Code = H.CustomerCode
WHERE H.RecStatus = 0
AND H.DocumentTypeCode = '{this.DocumentTypeCode}'
{QuerySearch}
{FilterCustomerType}";

            }
            else if (ModuleCode == "CNDNSALES")
            {
                string QuerySearch = "";
                if (this.LookUpEdit_Search.EditValue.ToString() == "Date")
                    QuerySearch = this.GetConditionDate("H.CrDrDate", this.DateEdit_From, this.DateEdit_To);
                else
                    QuerySearch = this.GetCondition("H.CrDrNo", this.TextEdit_From, this.TextEdit_To);

                Query = $@"SELECT CAST(0 as bit) 'Select'
, H.DocumentModuleCode
, H.DocumentTypeCode
, H.RunNo
, H.CrDrDate AS TransactionDate
, H.CrDrNo AS DocNo
, H.PayReceiveDocIssueCode AS CustomerCode
, LTRIM(RTRIM(ISNULL(H.PayReceiveDocIssuePrefix,'') +' '+ ISNULL(H.PayReceiveDocIssueName,'') + ' '+ISNULL(H.PayReceiveDocIssueSuffix,''))) AS CustomerFullName
, CAST(1 as bit) AS IsSendMail
FROM CreditDebitNoteSalesInvoiceHeader H
LEFT JOIN CustomerMaster CM
ON CM.Code = H.PayReceiveDocIssueCode
WHERE H.RecStatus = 0
AND H.DocumentTypeCode = '{this.DocumentTypeCode}'
{QuerySearch}
{FilterCustomerType}";
            }
            else if (ModuleCode == "CBPR")
            {
                string QuerySearch = "";
                if (this.LookUpEdit_Search.EditValue.ToString() == "Date")
                    QuerySearch = this.GetConditionDate("H.PRDate", this.DateEdit_From, this.DateEdit_To);
                else
                    QuerySearch = this.GetCondition("H.PRNo", this.TextEdit_From, this.TextEdit_To);

                Query = $@"SELECT CAST(0 as bit) 'Select'
, H.DocumentModuleCode
, H.DocumentTypeCode
, H.RunNo
, H.PRDate AS TransactionDate
, H.PRNo AS DocNo
, H.PayReceiveCode AS CustomerCode
, LTRIM(RTRIM(ISNULL(H.PayReceivePrefix,'') +' '+ ISNULL(H.PayReceiveName,'') + ' '+ISNULL(H.PayReceiveSuffix,''))) AS CustomerFullName
, CAST(1 as bit) AS IsSendMail
FROM CashBankPRHeader H
LEFT JOIN CustomerMaster CM
ON CM.Code = H.PayReceiveCode
WHERE H.RecStatus = 0
AND H.DocumentTypeCode = '{this.DocumentTypeCode.Replace("Header_", "")}'
{QuerySearch}
{FilterCustomerType}";
            }
            else if (ModuleCode == "RC")
            {
                string QuerySearch = "";
                if (this.LookUpEdit_Search.EditValue.ToString() == "Date")
                    QuerySearch = this.GetConditionDate("H.ReceiptDate", this.DateEdit_From, this.DateEdit_To);
                else
                    QuerySearch = this.GetCondition("H.ReceiptNo", this.TextEdit_From, this.TextEdit_To);

                Query = $@"SELECT CAST(0 as bit) 'Select'
, 'RC' AS DocumentModuleCode
, H.ReceiptType AS DocumentTypeCode
, H.RunNo
, H.ReceiptDate AS TransactionDate
, H.ReceiptNo AS DocNo
, H.ReceivableCode AS CustomerCode
, LTRIM(RTRIM(ISNULL(H.ReceivablePrefix,'') +' '+ ISNULL(H.ReceivableName,'') + ' '+ISNULL(H.ReceivableSuffix,''))) AS CustomerFullName
, CAST(1 as bit) AS IsSendMail
FROM ReceiptHeader H
LEFT JOIN CustomerMaster CM
ON CM.Code = H.ReceivableCode
WHERE H.RecStatus = 0
AND H.ReceiptType = '{this.DocumentTypeCode}'
{QuerySearch}
{FilterCustomerType}";
            }
            else
            {
                throw new NotImplementedException();
            }
            this.DBSimpleObj.FillData(TBEtax, Query);
        }
        private void LoadCompany()
        {
            string Query = String.Format("SELECT T FROM Company WHERE Code = '{0}'", this.QEBCenterInfo.Company);
            DataTable TBCompany = new DataTable();
            this.DBSimpleObj.FillData(TBCompany, Query, "QERP");
            if (TBCompany.Rows.Count > 0)
                SignCompanyName = ReceiveValue.StringReceive("T", TBCompany.Rows[0]);
        }
        private void btnSignETAX_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.Token.Password))
            {
                using (SettingForm form = new SettingForm(this.Token))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                        return;
                }
            }
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;
            string path = folderDialog.SelectedPath;
            int TransErr = 0;
            string ErrMsg = "";
            DataRow[] listSelect = this.TBEtax.AsEnumerable().Where(x => x.Field<bool>("Select")).ToArray();
            foreach (DataRow rowHeader in listSelect)
            {
                string ErrMsgInvoice = "";
                int TransErrInvoice = this.SignEtax(rowHeader, false, path, ref ErrMsgInvoice);
                if (TransErrInvoice != 0)
                {
                    TransErr++;
                    if (ErrMsg != "")
                        ErrMsg += Environment.NewLine;
                    ErrMsg += ErrMsgInvoice;
                }
            }
            if (TransErr == 0)
                MySuccessMessageBox.Show("Sign ETAX เสร็จเรียบร้อย");
            else
                MyMessageBox.Show(ErrMsg);
        }
        private int SignEtax(DataRow rowEtax, bool IsTestSign, string path, ref string ErrMsg)
        {
            int TransErr = 0;
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowEtax);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowEtax, -1);
            string DocNo = ReceiveValue.StringReceive("DocNo", rowEtax);
            bool IsSendMail = ReceiveValue.BoolReceive("IsSendMail", rowEtax, true);
            string FieldDocTypeCode = "DocumentTypeCode";
            DataTable TBSelect = null;
            if (ModuleCode.StartsWith("SI"))
                TBSelect = this.TBSIHeader.Clone();
            else if (ModuleCode == "CNDNSALES")
                TBSelect = this.TBCNDNSalesHeader.Clone();
            else if (ModuleCode == "CBPR")
                TBSelect = this.TBCBPRHeader.Clone();
            else if (ModuleCode == "RC")
            {
                TBSelect = this.TBRCHeader.Clone();
                FieldDocTypeCode = "ReceiptType";
            }
                
            else
            {
                throw new NotImplementedException();
            }
            string Query = $"SELECT * FROM {TBSelect.TableName} WHERE RecStatus = 0 AND {FieldDocTypeCode} = '{DocumentTypeCode}' AND RunNo = {RunNo}";
            this.DBSimpleObj.FillData(TBSelect, Query);
            if (TBSelect.Rows.Count == 0)
            {
                ErrMsg = $"ไม่พบข้อมูล";
                return ++TransErr;
            }
            Exception ex = null;
            DataRow rowHeader = TBSelect.Rows[0];
            DateTime DateSign = DateTime.Now;
            string DocNoSign = DocNo.Replace("/", "-");

            string filepathPDF = Path.Combine(path, $"{DocNoSign}.pdf");
            string filepathXML = Path.Combine(path, $"{DocNoSign}.xml");

            if (TransErr == 0 && this.UseEtax)
                if (IsTestSign)
                    TransErr = this.SignXML_Test(rowHeader, filepathXML, ref ErrMsg);
                else
                    TransErr = this.SignXML(rowHeader, filepathXML, ref ErrMsg);

            if (TransErr == 0)
                TransErr = this.SignPDF(this.DataSetObj
                        , this.ReportObj
                        , rowHeader
                        , this.TableNameDetails
                        , this.FIELD_DocNo
                        , this.PrintInsertRow
                        , DateSign
                        , filepathPDF
                        , IsTestSign
                        , ref ErrMsg);

            if (TransErr == 0 && !IsTestSign)
                TransErr += this.UploadFile(rowHeader, DateSign, filepathPDF, filepathXML, ref ErrMsg);
            if (TransErr == 0)
            {
                string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
                if (DocumentModuleCode == "SITI" && IsSendMail)
                {
                    DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader);
                    string CustomerCode = ReceiveValue.StringReceive("CustomerCode", rowHeader);
                    TransErr += this.EtaxMail.AutoSendMail(DocumentModuleCode, DocNo, CustomerCode, filepathPDF, ref ErrMsg, ref ex);
                }
            }
            if (TransErr != 0)
                ErrMsg = $"เลขที่เอกสาร : {DocNo} เกิดข้อผิดพลาด : {ErrMsg}";
            return TransErr;
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private int SignPDF(DataSet DataSetObj
         , XtraReport ReportObj
         , DataRow rowHeader
         , string TableNameDetails
         , string FIELD_DocNo
         , UtilityHelper.PrintUtil.EnumPrint PrintInsertRow
         , DateTime DateSign
         , string FileName
         , bool IsTestSign
         , ref string ErrMsg)
        {
            int TransErr = 0;
            int ErrCode = 0;
            DataSet DSSignObj = DataSetObj.Clone();

            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            ReportObj.DataSourceSchema = "";
            ReportObj.SaveLayout(stream);

            XtraReport reportSign = new XtraReport();
            reportSign.LoadLayout(stream);
            reportSign.DataSourceSchema = "";
            var param = new DevExpress.XtraReports.Parameters.Parameter();
            param.Name = "UseEtax";
            param.ParameterType = DevExpress.XtraReports.Parameters.ParameterType.Boolean;
            param.Value = true;
            param.Visible = false;
            reportSign.Parameters.Add(param);

            bool Cancel = false;
            if (TransErr == 0)
                TransErr += this.CallbackPrintPreview(DSSignObj, ref reportSign, UtilityHelper.PrintUtil.EnumPrintType.Preview, PrintInsertRow, rowHeader, FIELD_DocNo, TableNameDetails, ref Cancel, ref ErrCode, ref ErrMsg);
            if (TransErr == 0 && !Cancel)
            {
                try
                {
                    XRControl ctr = ReportObj.FindControl("ETAX_SIGN", false);
                    if (ctr == null || ctr.Tag == null)
                    {
                        TransErr++;
                        ErrMsg = "ยังไม่ได้ระบุตำแหน่งของ Sign PDF ที่ Report(ETAX_SIGN)";
                    }
                    else
                    {
                        float x = 0, y = 0, width = 0, height = 0;
                        if (ctr.Tag is String)
                        {
                            string[] value = ctr.Tag.ToString().Split(',');
                            if (value.Length == 4)
                            {
                                float.TryParse(value[0], out x);
                                float.TryParse(value[1], out y);
                                float.TryParse(value[2], out width);
                                float.TryParse(value[3], out height);
                            }
                        }
                        string filetemp = String.Format("{0}{1}.pdf", System.IO.Path.GetTempPath(), Guid.NewGuid());
                        reportSign.ExportToPdf(filetemp);
                        EtaxSignUtil.SignPDF signpdf = new EtaxSignUtil.SignPDF(this.DBSimpleObj, this.QEBCenterInfo);
                        TransErr += signpdf.SignWithCert(this.UseEtax, this.Token, SignCompanyName, DateSign, FileName, filetemp, x, y, width, height, IsTestSign, ref ErrMsg);
                        try
                        {
                            if (File.Exists(filetemp))
                                File.Delete(filetemp);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    ErrMsg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        ErrMsg += Environment.NewLine;
                        ErrMsg += ex.InnerException.Message;
                    }
                    TransErr++;
                }

            }
            return TransErr;
        }
        private int SignXML(DataRow rowHeader, string filepath, ref string ErrMsg)
        {
            int TransErr = 0;
            string filetemp = $"{System.IO.Path.GetTempPath()}{Guid.NewGuid()}.xml";
            try
            {
                string DocumentType = "";
                string input = "";
                TransErr = this.GetXMLLayout(rowHeader, ref DocumentType, ref input, ref ErrMsg);
                if (TransErr == 0)
                {
                    File.WriteAllText(filetemp, input, Encoding.UTF8);
                    string jarFile = Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), this.Token.Java_FileName);
                    string jreFile = "\"java\"";
                    if (TransErr == 0)
                    {
                        TransErr = this.GetOutputJava(jreFile, jarFile, filetemp, filepath, ref ErrMsg);
                        if (TransErr != 0 && (ErrMsg == "" || ErrMsg.Contains("The system cannot find the file specified")))
                        {
                            string FolderPath = @"C:\Program Files (x86)\Java";
                            if (Directory.Exists(FolderPath))
                            {
                                string[] subdirs = Directory.GetDirectories(FolderPath);
                                foreach (string javaPath in subdirs)
                                {
                                    ErrMsg = "";
                                    jreFile = @"{0}\bin\java.exe";
                                    jreFile = String.Format(jreFile, javaPath);
                                    TransErr = this.GetOutputJava(jreFile, jarFile, filetemp, filepath, ref ErrMsg);
                                    if (TransErr == 0)
                                        break;
                                }
                            }
                        }
                        if (TransErr != 0 && (ErrMsg == "" || ErrMsg.Contains("The system cannot find the file specified")))
                        {
                            string FolderPath = @"C:\Program Files\Java";
                            if (Directory.Exists(FolderPath))
                            {
                                string[] subdirs = Directory.GetDirectories(FolderPath);
                                foreach (string javaPath in subdirs)
                                {
                                    ErrMsg = "";
                                    jreFile = @"{0}\bin\java.exe";
                                    jreFile = String.Format(jreFile, javaPath);
                                    TransErr = this.GetOutputJava(jreFile, jarFile, filetemp, filepath, ref ErrMsg);
                                    if (TransErr == 0)
                                        break;
                                }
                            }
                        }
                    }
                    if (TransErr != 0)
                    {
                        TransErr = ResourceLoader.CreatedEmbeddedResourceToTempPath(this.Token.Java_FileName,ref jarFile);
                        if (TransErr == 0)
                        {
                            TransErr = this.GetOutputJava(jreFile, jarFile, filetemp, filepath, ref ErrMsg);
                        }
                    }
                    if (TransErr != 0)
                    {
                        if (String.IsNullOrEmpty(ErrMsg))
                        {
                            ErrMsg = $@"Can't Run Java
Java Runtime Environment Path : {jreFile}
Jar Path : {jarFile}";
                        }
                        else if (ErrMsg.Contains("The keystore couldn't be initialized"))
                            ErrMsg = "ไม่พบ USB TOKEN  OR Password Wrong";
                        else
                            ErrMsg = $"Java Error Message : {ErrMsg}";
                    }
                }

            }
            catch (Exception ex)
            {
                TransErr++;
                if (ex.Message == "The system cannot find the file specified")
                    ErrMsg = $@"กรุณาติดตั้ง Java RunTime และไฟล์ {this.Token.Java_FileName}
Description : {ex.Message}";
                else
                    ErrMsg = ex.Message;
            }
            if (File.Exists(filetemp))
                File.Delete(filetemp);

            return TransErr;
        }
        private int GetOutputJava(string jreFile, string jarFile, string filetemp, string filepath, ref string ErrMsg)
        {
            int TransErr = 0;
            try
            {
                string strArguments = $@" -jar ""{jarFile}"" {Token.Type} {Token.Java_ProviderName} {Token.Password} {Token.LibPath} {filetemp.Replace(" ", "[]")} {filepath.Replace(" ", "[]")}";

                System.Diagnostics.Process processJar = new System.Diagnostics.Process();
                processJar.StartInfo.CreateNoWindow = true;
                processJar.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                processJar.StartInfo.FileName = jreFile;
                processJar.StartInfo.Arguments = strArguments;
                processJar.StartInfo.UseShellExecute = false;
                processJar.StartInfo.RedirectStandardOutput = true;
                processJar.Start();
                string result = processJar.StandardOutput.ReadToEnd();
                processJar.WaitForExit();

                string[] ListResult = result.Split(new string[] { "|", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (ListResult.Length == 2)
                {
                    bool IsSigner = Convert.ToBoolean(ListResult[0]);
                    string Message = ListResult[1];
                    if (!IsSigner)
                    {
                        TransErr++;
                        ErrMsg = Message;
                    }
                }
                else
                {
                    TransErr++;
                    ErrMsg = result;
                }
            }
            catch (Exception ex)
            {
                TransErr++;

                if (ex.InnerException == null)
                    ErrMsg = ex.Message;
                else
                {
                    ErrMsg =
$@"Message : {ex.Message}
Description : {ex.InnerException.Message}";
                }
            }
            return TransErr;
        }
        private int GetXMLLayout(DataRow rowHeader, ref string DocumentType, ref string XMLLayout, ref string ErrMsg)
        {
            int TransErr = 0;
            string ModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
            if (ModuleCode == "SITI")
            {
                SILayout SILayoutObj = new SILayout(this.DBSimpleObj, this.QEBCenterInfo);
                TransErr += SILayoutObj.InitLayout(rowHeader, ref ErrMsg);
                if (TransErr == 0)
                    TransErr = SILayoutObj.ValidateLayout(ref ErrMsg);
                if (TransErr == 0)
                    XMLLayout = SILayoutObj.GetXMLLayout();
                DocumentType = "Tax Invoice";
            }
            else if (ModuleCode == "CNDNSALES")
            {
                CNDNSALES.CNDNSALESLayout CNDNLayoutObj = new CNDNSALES.CNDNSALESLayout(this.DBSimpleObj, this.QEBCenterInfo);
                TransErr += CNDNLayoutObj.InitLayout(rowHeader, ref ErrMsg);
                if (TransErr == 0)
                    TransErr = CNDNLayoutObj.ValidateLayout(ref ErrMsg);
                if (TransErr == 0)
                    XMLLayout = CNDNLayoutObj.GetXMLLayout();
                DocumentType = "Credit Note";
                string CrDr = ReceiveValue.StringReceive("CrDr", rowHeader);
                if (CrDr == "C")
                    DocumentType = "Debit Note";
            }
            else if (ModuleCode == "CBPR")
            {
                RC_INV.PRLayout PRLayoutObj = new RC_INV.PRLayout(this.DBSimpleObj, this.QEBCenterInfo);
                TransErr += PRLayoutObj.InitLayout(rowHeader, ref ErrMsg);
                if (TransErr == 0)
                    TransErr = PRLayoutObj.ValidateLayout(ref ErrMsg);
                if (TransErr == 0)
                    XMLLayout = PRLayoutObj.GetXMLLayout();
                DocumentType = "Tax Invoice";
            }
            else if (ModuleCode == "RC")
            {
                RC_INV.RCLayout RCLayoutObj = new RC_INV.RCLayout(this.DBSimpleObj, this.QEBCenterInfo);
                TransErr += RCLayoutObj.InitLayout(rowHeader, ref ErrMsg);
                if (TransErr == 0)
                    TransErr = RCLayoutObj.ValidateLayout(ref ErrMsg);
                if (TransErr == 0)
                    XMLLayout = RCLayoutObj.GetXMLLayout();
                DocumentType = "Receipt";
            }
            return TransErr;
        }
        private int UploadFile(DataRow rowHeader, DateTime DateSign, string filepathPDF, string filepathXML, ref string ErrMsg)
        {
            int TransErr = 0;
            SqlConnectionStringBuilder conBuilder = new SqlConnectionStringBuilder();
            conBuilder.DataSource = this.QEBCenterInfo.Server;
            conBuilder.InitialCatalog = this.QEBCenterInfo.Company;
            conBuilder.UserID = this.QEBCenterInfo.Username;
            conBuilder.Password = this.QEBCenterInfo.Password;
            conBuilder.PersistSecurityInfo = true;
            string constring = conBuilder.ConnectionString;
            try
            {
                using (SqlConnection con = new SqlConnection(constring))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        this.InsertFileDoc(con, tran, filepathPDF, "ETAX_PDF", DateSign, rowHeader);
                        if (this.UseEtax && !String.IsNullOrEmpty(filepathXML))
                            this.InsertFileDoc(con, tran, filepathXML, "ETAX_XML", DateSign, rowHeader);
                        tran.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                TransErr++;
                ErrMsg = ex.Message;
            }
            return TransErr;
        }
        private void InsertFileDoc(SqlConnection con, SqlTransaction tran, string filepath, string etaxType, DateTime DateSign, DataRow rowHeader)
        {
            string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowHeader);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowHeader, -1);
            string filename = Path.GetFileName(filepath);
            byte[] fileData = null;
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader rdr = new BinaryReader(fs))
                {
                    fileData = rdr.ReadBytes((int)fs.Length);
                    rdr.Close();
                    fs.Close();
                }
            }
            if (fileData != null)
            {
                string QueryInsert = @"INSERT INTO FileDoc(DocumentModuleCode,
DocumentTypeCode,
RunNo,
Id,
RefTableHeaderDetails,
RefDocumentRunNoDocNo,
RefDocumentNolineDocNo,
FileData,
NameFile,
DescriptionFile,
RecStatus,
LastUpdated,
UpdatedUser)
VALUES(
@DocumentModuleCode,
@DocumentTypeCode,
@RunNo,
NEWID(),
@RefTableHeaderDetails,
@RefDocumentRunNoDocNo,
@RefDocumentNolineDocNo,
@FileData,
@NameFile,
@DescriptionFile,
@RecStatus,
@LastUpdated,
@UpdatedUser)";
                using (SqlCommand command = new SqlCommand(QueryInsert, con, tran))
                {
                    command.CommandTimeout = CommandTimeout;
                    command.Parameters.AddWithValue("@DocumentModuleCode", DocumentModuleCode);
                    command.Parameters.AddWithValue("@DocumentTypeCode", DocumentTypeCode);
                    command.Parameters.AddWithValue("@RunNo", RunNo);
                    command.Parameters.AddWithValue("@RefTableHeaderDetails", etaxType);
                    command.Parameters.AddWithValue("@RefDocumentRunNoDocNo", DBNull.Value);
                    command.Parameters.AddWithValue("@RefDocumentNolineDocNo", DBNull.Value);
                    command.Parameters.AddWithValue("@FileData", fileData);
                    command.Parameters.AddWithValue("@NameFile", filename);
                    command.Parameters.AddWithValue("@DescriptionFile", DBNull.Value);
                    command.Parameters.AddWithValue("@RecStatus", 0);
                    command.Parameters.AddWithValue("@LastUpdated", DateSign);
                    command.Parameters.AddWithValue("@UpdatedUser", this.QEBCenterInfo.Username);
                    command.ExecuteNonQuery();
                }
            }
        }
        private void ButtonSettingToken_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SettingForm form = new SettingForm(this.Token))
            {
                form.ShowDialog();
            }
        }
        public string GetConditionDate(string FileName, DateEdit datefrom, DateEdit dateto)
        {
            string DateSearch = string.Empty;
            string DateFromtxt = datefrom.Text;
            string DateTotxt = dateto.Text;
            if (DateFromtxt != "")
                DateSearch += $"AND {FileName} >= '{GetQueryText(datefrom.DateTime)}'";
            if (DateTotxt != "")
                DateSearch += $"AND {FileName} < '{GetQueryText(dateto.DateTime.AddDays(1))}'";
            return DateSearch;
        }
        public string GetCondition(string FileName, TextEdit txtfrom, TextEdit txtTo)
        {
            string Query = string.Empty;
            string Fromtxt = txtfrom.Text;
            string Totxt = txtTo.Text;
            if (Fromtxt != "")
                Query += $"AND {FileName} >= '{Fromtxt}'";
            if (Totxt != "")
                Query += $"AND {FileName} <= '{Totxt}'";
            return Query;
        }
        private void toolStripMenuItemSelectAll_Click(object sender, EventArgs e)
        {
            foreach (DataRow rowEtax in this.TBEtax.Rows)
            {
                rowEtax.BeginEdit();
                rowEtax["Select"] = true;
                rowEtax.EndEdit();
            }
        }
        private void toolStripMenuItemUnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (DataRow rowEtax in this.TBEtax.Rows)
            {
                rowEtax.BeginEdit();
                rowEtax["Select"] = false;
                rowEtax.EndEdit();
            }
        }
        private void RepositoryItemButtonDetails_ButtonPressed(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            DataRow rowFocus = this.gridView.GetFocusedDataRow();
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowFocus);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowFocus, -1);
            string DocNo = ReceiveValue.StringReceive("DocNo", rowFocus);

            string FieldDocTypeCode = "DocumentTypeCode";
            DataTable TBSelect = null;
            if (ModuleCode.StartsWith("SI"))
                TBSelect = this.TBSIHeader.Clone();
            else if (ModuleCode == "CNDNSALES")
                TBSelect = this.TBCNDNSalesHeader.Clone();
            else if (ModuleCode == "CBPR")
                TBSelect = this.TBCBPRHeader.Clone();
            else if (ModuleCode == "RC")
            {
                TBSelect = this.TBRCHeader.Clone();
                FieldDocTypeCode = "ReceiptType";
            }
            else
            {
                throw new NotImplementedException();
            }
            string Query = $"SELECT * FROM {TBSelect.TableName} WHERE RecStatus = 0 AND {FieldDocTypeCode} = '{DocumentTypeCode}' AND RunNo = {RunNo}";
            this.DBSimpleObj.FillData(TBSelect, Query);
            if (TBSelect.Rows.Count == 0)
            {
                return;
            }
            DataRow rowHeader = TBSelect.Rows[0];
            using (SignETAXForm signETAX = new SignETAXForm(this.QEBCenterInfo
                , this.DBSimpleObj
                , CallbackPrintPreview
                , DataSetObj
                , ReportObj
                , rowHeader
                , TableNameDetails
                , FIELD_DocNo
                , PrintInsertRow))
            {
                signETAX.ShowPopup();
            }
        }
        private int SignXML_Test(DataRow rowHeader, string filepath, ref string ErrMsg)
        {
            int TransErr = 0;
            try
            {
                string DocumentType = "";
                string input = "";
                TransErr = this.GetXMLLayout(rowHeader, ref DocumentType, ref input, ref ErrMsg);
                if (TransErr == 0)
                    File.WriteAllText(filepath, input, Encoding.UTF8);

            }
            catch (Exception ex)
            {
                TransErr++;
                ErrMsg = ex.Message;
            }

            return TransErr;
        }
        private void barButtonItemTest_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;
            string path = folderDialog.SelectedPath;
            int TransErr = 0;
            string ErrMsg = "";
            DataRow[] listSelect = this.TBEtax.AsEnumerable().Where(x => x.Field<bool>("Select")).ToArray();
            foreach (DataRow rowHeader in listSelect)
            {
                string ErrMsgInvoice = "";
                int TransErrInvoice = this.SignEtax(rowHeader, true, path, ref ErrMsgInvoice);
                if (TransErrInvoice != 0)
                {
                    TransErr++;
                    if (ErrMsg != "")
                        ErrMsg += Environment.NewLine;
                    ErrMsg += ErrMsgInvoice;
                }
            }
            if (TransErr == 0)
                MySuccessMessageBox.Show("Sign ETAX เสร็จเรียบร้อย");
            else
                MyMessageBox.Show(ErrMsg);
        }
        private void LookUpEdit_CustomerType_EditValueChanged(object sender, EventArgs e)
        {
            this.LoadData();
        }
        private void BarButtonItemTest_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;
            string path = folderDialog.SelectedPath;
            int TransErr = 0;
            string ErrMsg = "";
            DataRow[] listSelect = this.TBEtax.AsEnumerable().Where(x => x.Field<bool>("Select")).ToArray();
            foreach (DataRow rowHeader in listSelect)
            {
                string ErrMsgInvoice = "";
                int TransErrInvoice = this.SignPDFA3(rowHeader, true, path, ref ErrMsgInvoice);
                if (TransErrInvoice != 0)
                {
                    TransErr++;
                    if (ErrMsg != "")
                        ErrMsg += Environment.NewLine;
                    ErrMsg += ErrMsgInvoice;
                }
            }
            if (TransErr == 0)
                MySuccessMessageBox.Show("Test Gen PDF/A-3 เสร็จเรียบร้อย");
            else
                MyMessageBox.Show(ErrMsg);
        }
        private void BtnSignPDF_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;
            string path = folderDialog.SelectedPath;
            int TransErr = 0;
            string ErrMsg = "";
            DataRow[] listSelect = this.TBEtax.AsEnumerable().Where(x => x.Field<bool>("Select")).ToArray();
            foreach (DataRow rowHeader in listSelect)
            {
                string ErrMsgInvoice = "";
                int TransErrInvoice = this.SignPDFA3(rowHeader, false, path, ref ErrMsgInvoice);
                if (TransErrInvoice != 0)
                {
                    TransErr++;
                    if (ErrMsg != "")
                        ErrMsg += Environment.NewLine;
                    ErrMsg += ErrMsgInvoice;
                }
            }
            if (TransErr == 0)
                MySuccessMessageBox.Show("Gen PDF/A-3 เสร็จเรียบร้อย");
            else
                MyMessageBox.Show(ErrMsg);
        }
        private int SignPDFA3(DataRow rowEtax, bool IsTestSign, string path, ref string ErrMsg)
        {
            int TransErr = 0;
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowEtax);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowEtax, -1);
            string DocNo = ReceiveValue.StringReceive("DocNo", rowEtax);
            bool IsSendMail = ReceiveValue.BoolReceive("IsSendMail", rowEtax, true);
            DataTable TBSelect = null;
            if (ModuleCode.StartsWith("SI"))
                TBSelect = this.TBSIHeader.Clone();
            else if (ModuleCode == "CNDNSALES")
                TBSelect = this.TBCNDNSalesHeader.Clone();
            else if (ModuleCode == "CBPR")
                TBSelect = this.TBCBPRHeader.Clone();
            else if (ModuleCode == "RC")
                TBSelect = this.TBRCHeader.Clone();
            else
            {
                throw new NotImplementedException();
            }
            string Query = $"SELECT * FROM {TBSelect.TableName} WHERE RecStatus = 0 AND DocumentTypeCode = '{DocumentTypeCode}' AND RunNo = {RunNo}";
            this.DBSimpleObj.FillData(TBSelect, Query);
            if (TBSelect.Rows.Count == 0)
            {
                ErrMsg = $"ไม่พบข้อมูล";
                return ++TransErr;
            }
            Exception ex = null;
            DataRow rowHeader = TBSelect.Rows[0];
            DateTime DateSign = DateTime.Now;
            string DocNoSign = DocNo.Replace("/", "-");
            string DocumentType = "";
            string XMLData = "";

            string filepathPDF = Path.Combine(path, $"{DocNoSign}.pdf");

            TransErr = this.GetXMLLayout(rowHeader, ref DocumentType, ref XMLData, ref ErrMsg);

            if (TransErr == 0)
                TransErr = this.GenPDF3A(this.DataSetObj
                        , this.ReportObj
                        , rowHeader
                        , this.TableNameDetails
                        , this.FIELD_DocNo
                        , this.PrintInsertRow
                        , DateSign
                        , filepathPDF
                        , XMLData
                        , DocumentType
                        , ref ErrMsg
                        , ref ex);

            if (TransErr == 0 && !IsTestSign)
                TransErr += this.UploadFile(rowHeader, DateSign, filepathPDF, "", ref ErrMsg);
            if (TransErr == 0)
            {
                string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
                if (DocumentModuleCode == "SITI" && IsSendMail)
                {
                    DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader);
                    string CustomerCode = ReceiveValue.StringReceive("CustomerCode", rowHeader);
                    TransErr += this.EtaxMail.AutoSendMail(DocumentModuleCode, DocNo, CustomerCode, filepathPDF, ref ErrMsg, ref ex);
                }
            }
            if (TransErr != 0)
                ErrMsg = $"เลขที่เอกสาร : {DocNo} เกิดข้อผิดพลาด : {ErrMsg}";
            return TransErr;
        }
        private int GenPDF3A(DataSet DataSetObj
          , XtraReport ReportObj
          , DataRow rowHeader
          , string TableNameDetails
          , string FIELD_DocNo
          , UtilityHelper.PrintUtil.EnumPrint PrintInsertRow
          , DateTime DateSign
          , string FileName
          , string XMLData
          , string DocumentType
          , ref string ErrMsg
          , ref Exception ErrException)
        {
            int TransErr = 0;
            int ErrCode = 0;
            DataSet DSSignObj = DataSetObj.Clone();

            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            ReportObj.DataSourceSchema = "";
            ReportObj.SaveLayout(stream);

            XtraReport reportSign = new XtraReport();
            reportSign.LoadLayout(stream);
            reportSign.DataSourceSchema = "";
            var param = new DevExpress.XtraReports.Parameters.Parameter();
            param.Name = "UseEtax";
            param.ParameterType = DevExpress.XtraReports.Parameters.ParameterType.Boolean;
            param.Value = true;
            param.Visible = false;
            reportSign.Parameters.Add(param);

            bool Cancel = false;
            if (TransErr == 0)
                TransErr += this.CallbackPrintPreview(DSSignObj, ref reportSign, UtilityHelper.PrintUtil.EnumPrintType.Preview, PrintInsertRow, rowHeader, FIELD_DocNo, TableNameDetails, ref Cancel, ref ErrCode, ref ErrMsg);
            if (TransErr == 0 && !Cancel)
            {
                try
                {
                    string pdfFilePath = String.Format("{0}{1}.pdf", System.IO.Path.GetTempPath(), Guid.NewGuid());
                    string xmlFilePath = String.Format("{0}{1}.xml", System.IO.Path.GetTempPath(), Guid.NewGuid());
                    reportSign.ExportToPdf(pdfFilePath);
                    File.WriteAllText(xmlFilePath, XMLData, Encoding.UTF8);
                    new SignPDF(this.DBSimpleObj, this.QEBCenterInfo).GenPDF3A(pdfFilePath, xmlFilePath, FileName, DocumentType, ref ErrMsg, ref ErrException);
                    try
                    {
                        if (File.Exists(pdfFilePath))
                            File.Delete(pdfFilePath);
                        if (File.Exists(xmlFilePath))
                            File.Delete(xmlFilePath);
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    ErrException = ex;
                    TransErr++;
                    ErrMsg = ex.Message;
                }

            }
            return TransErr;
        }

        private void btnSendAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < gridView.RowCount; i++)
            {
                gridView.SetRowCellValue(i, "IsSendMail", true);
            }
        }

        private void btnCancelAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < gridView.RowCount; i++)
            {
                gridView.SetRowCellValue(i, "IsSendMail", false);
            }
        }
        
    }
}
