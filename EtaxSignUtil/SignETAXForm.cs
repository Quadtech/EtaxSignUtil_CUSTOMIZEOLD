using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
using System.Data.SqlClient;
using MessageUtil;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.xml.simpleparser;

namespace EtaxSignUtil
{
    public partial class SignETAXForm : System.Windows.Forms.Form
    {
        private const int CommandTimeout = 300;
        private Center QEBCenterInfo { get; set; }
        private DBSimple DBSimpleObj { get; set; }
        private string SignCompanyName { get; set; } = "";
        private DelegatePrintPreview CallbackPrintPreview { get; set; }
        private DataSet DataSetObj { get; set; }
        private XtraReport ReportObj { get; set; }
        private DataRow rowHeader { get; set; }
        private string FIELD_DocNo { get; set; }
        private string TableNameDetails { get; set; }
        private UtilityHelper.PrintUtil.EnumPrint PrintInsertRow { get; set; }
        private DataTable TBEtax { get; set; }
        private bool UseEtax { get; set; } = true;
        private EtaxMail EtaxMail { get; set; }
        private TokenInfo Token { get; set; }
        private bool UsePdfA3 { get; set; } = false;
        public SignETAXForm(Center QEBCenterInfo
            , DBSimple DBSimpleObj
            , DelegatePrintPreview CallbackPrintPreview
            , DataSet DataSetObj
            , XtraReport ReportObj
            , DataRow rowHeader
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
            this.rowHeader = rowHeader;
            this.FIELD_DocNo = FIELD_DocNo;
            this.TableNameDetails = TableNameDetails;
            this.PrintInsertRow = PrintInsertRow;
            this.InitTable();
            this.InitControl();
            this.LoadCompany();
            ResourceLoader.DeleteFileTempPath();
        }
        private void InitTable()
        {
            this.TBEtax = new DataTable();
            this.TBEtax.Columns.Add("ID", typeof(Int32));
            this.TBEtax.Columns.Add("LastUpdated", typeof(DateTime));
            this.TBEtax.Columns.Add("FileData", typeof(Byte[]));
            this.TBEtax.Columns.Add("XMLFileData", typeof(Byte[]));
            this.TBEtax.Columns.Add("UpdatedUser");

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


            this.gridControl.DataSource = new DataView(this.TBEtax, "", "ID ASC", DataViewRowState.CurrentRows);
            this.repositoryItemButtonPreview.ButtonPressed += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(repositoryItemButtonPreview_ButtonPressed);

            string username = this.QEBCenterInfo.Username.ToLower();
            if (username == "sa" || username == "testuser" || username == "qerpsa")
                ButtonTestReport.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;

            this.EtaxMail = new EtaxMail(this.QEBCenterInfo, this.DBSimpleObj);
            this.EtaxMail.InitData();
            if (!this.EtaxMail.UseEtaxSendmail)
                this.layoutControlItemAutoSendMail.HideToCustomization();

            if (this.UsePdfA3)
            {
                this.ButtonSettingToken.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                this.btnSignPDF.Text = "PDF/A-3";
                this.btnSignPDF.Click += this.SignPDF3A_Click;
                this.ButtonTestReport.ItemClick += TestPDF_ItemClick;
            }
            else
            {
                this.btnSignPDF.Click += this.SignETAX_Click;
                this.ButtonTestReport.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.ButtonTestReport_ItemClick);
            }
        }
        private void repositoryItemButtonPreview_ButtonPressed(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            DataRow rowFocus = this.gridView.GetFocusedDataRow();
            if (rowFocus == null)
                return;
            try
            {
                if (e.Button.Index == 0)
                {
                    string filetemp = String.Format("{0}{1}.pdf", System.IO.Path.GetTempPath(), Guid.NewGuid());
                    byte[] FileData = (byte[])rowFocus["FileData"];
                    if (FileData.Length == 0)
                        return;
                    File.WriteAllBytes(filetemp, FileData);
                    try
                    {
                        ProcessStartInfo info = new ProcessStartInfo(filetemp)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(info);
                    }
                    catch { }
                }
                else if (e.Button.Index == 1)
                {
                    string filetemp = String.Format("{0}{1}.xml", System.IO.Path.GetTempPath(), Guid.NewGuid());
                    byte[] FileData = (byte[])rowFocus["XMLFileData"];
                    if (FileData.Length == 0)
                        return;
                    File.WriteAllBytes(filetemp, FileData);
                    try
                    {
                        ProcessStartInfo info = new ProcessStartInfo(filetemp)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(info);
                    }
                    catch { }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void LoadETAX()
        {
            string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
            string DocumentTypeCode = ReceiveValue.StringReceive("DocumentTypeCode", rowHeader);
            long RunNo = ReceiveValue.LongReceive("RunNo", rowHeader, -1);
            string Query =
@"SELECT ROW_NUMBER() OVER (ORDER BY F.LastUpdated) AS ID
,F.FileData
,X.XMLFileData
,F.LastUpdated 
,F.UpdatedUser
FROM FileDoc F
OUTER APPLY 
(
	SELECT TOP 1 X.FileData AS XMLFileData FROM FileDoc X
	WHERE X.DocumentModuleCode = F.DocumentModuleCode
	AND X.DocumentTypeCode = F.DocumentTypeCode
	AND X.RunNo = F.RunNo
	AND DATEADD(MINUTE, DATEDIFF(MINUTE, 0, X.LastUpdated), 0) = DATEADD(MINUTE, DATEDIFF(MINUTE, 0, F.LastUpdated), 0)
	AND X.RefTableHeaderDetails = 'ETAX_XML'
) X
WHERE F.RefTableHeaderDetails = 'ETAX_PDF'
AND F.DocumentModuleCode = '{0}'
AND F.DocumentTypeCode = '{1}'
AND F.RunNo = {2}";
            Query = String.Format(Query, DocumentModuleCode, DocumentTypeCode, RunNo);
            this.DBSimpleObj.FillData(this.TBEtax, Query);
        }
        public DialogResult ShowPopup()
        {
            string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
            if (DocumentModuleCode == "SIDO")
                this.UseEtax = false;
            this.LoadETAX();
            if (this.UseEtax && !this.UsePdfA3)
                this.repositoryItemButtonPreview.Buttons[1].Visible = true;
            else
                this.repositoryItemButtonPreview.Buttons[1].Visible = false;
            return this.ShowDialog();
        }
        private void LoadCompany()
        {
            string Query = String.Format("SELECT T FROM Company WHERE Code = '{0}'", this.QEBCenterInfo.Company);
            DataTable TBCompany = new DataTable();
            this.DBSimpleObj.FillData(TBCompany, Query, "QERP");
            if (TBCompany.Rows.Count > 0)
                SignCompanyName = ReceiveValue.StringReceive("T", TBCompany.Rows[0]);
        }
        private void SignETAX_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.Token.Password))
            {
                using (SettingForm form = new SettingForm(this.Token))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                        return;
                }
            }
            string ErrMsg = "";
            DateTime DateSign = DateTime.Now;
            string DocNo = "";
            if (rowHeader.Table.Columns.Contains(FIELD_DocNo))
            {
                DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader).Replace("/", "-");
                int RecStatus = ReceiveValue.IntReceive("RecStatus", rowHeader, 0);
                if (RecStatus == 1)
                    DocNo += "(ยกเลิก)";
            }
            Exception ex = null;
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save ETAX";
            saveDialog.Filter = "ETAX(*.etax)|*.etax";
            saveDialog.FileName = DocNo;
            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;
            string filepathPDF = saveDialog.FileName.Replace(".etax", ".pdf");
            string filepathXML = saveDialog.FileName.Replace(".etax", ".xml");

            int TransErr = 0;
            if (TransErr == 0 && this.UseEtax)
                TransErr = this.SignXML(filepathXML, ref ErrMsg, ref ex);

            if (TransErr == 0)
                TransErr = this.SignPDF(this.DataSetObj
                        , this.ReportObj
                        , this.rowHeader
                        , this.TableNameDetails
                        , this.FIELD_DocNo
                        , this.PrintInsertRow
                        , DateSign
                        , filepathPDF
                        , false
                        , ref ErrMsg
                        , ref ex);

            if (TransErr == 0)
                TransErr += this.UploadFile(this.rowHeader, DateSign, filepathPDF, filepathXML, ref ErrMsg, ref ex);
            if (TransErr == 0)
            {
                string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
                if (DocumentModuleCode == "SITI" && this.CheckEdit_AutoSendMail.Checked)
                {
                    DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader);
                    string CustomerCode = ReceiveValue.StringReceive("CustomerCode", rowHeader);
                    TransErr += this.EtaxMail.AutoSendMail(DocumentModuleCode, DocNo, CustomerCode, filepathPDF, ref ErrMsg, ref ex);
                }
            }
            if (TransErr == 0)
            {
                MySuccessMessageBox.Show("Sign ETAX เสร็จเรียบร้อย");
                try
                {
                    System.Diagnostics.Process.Start(filepathPDF);
                }
                catch { }

                this.LoadETAX();
            }
            else
            {
                if (ex != null)
                {
                    string ExcaptionError = "";
                    if (ex.InnerException != null)
                    {
                        ExcaptionError = $@"InnerException Message : {ex.InnerException.Message}
StackTrace : {ex.StackTrace}";
                    }
                    else
                    {
                        ExcaptionError = ex.StackTrace;
                    }
                    MessageBox.Show(ErrMsg, ExcaptionError);
                }
                else
                    MyMessageBox.Show(ErrMsg);
            }
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
                    ErrException = ex;
                    TransErr++;
                    ErrMsg = ex.Message;
                }

            }
            return TransErr;
        }
        private int SignXML(string filepath, ref string ErrMsg, ref Exception ErrException)
        {
            int TransErr = 0;
            string filetemp = $"{System.IO.Path.GetTempPath()}{Guid.NewGuid()}.xml";
            try
            {
                string DocumentType = "";
                string input = "";
                TransErr = this.GetXMLLayout(ref DocumentType, ref input, ref ErrMsg);
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
                        TransErr = ResourceLoader.CreatedEmbeddedResourceToTempPath(this.Token.Java_FileName, ref jarFile);
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
                ErrException = ex;
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
        private int GetXMLLayout(ref string DocumentType, ref string XMLLayout, ref string ErrMsg)
        {
            int TransErr = 0;
            string ModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
            if (ModuleCode == "" && rowHeader.Table.TableName == "ReceiptHeader")
                ModuleCode = "RC";
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
                string CrDr = ReceiveValue.StringReceive("CrDr", rowHeader);
                DocumentType = "Credit Note";
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
        private int UploadFile(DataRow rowHeader, DateTime DateSign, string filepathPDF, string filepathXML, ref string ErrMsg, ref Exception ErrException)
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
                ErrException = ex;
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
        private void ButtonTestReport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string ErrMsg = "";
            DateTime DateSign = DateTime.Now;
            string DocNo = "";
            if (rowHeader.Table.Columns.Contains(FIELD_DocNo))
            {
                DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader).Replace("/", "-");
                int RecStatus = ReceiveValue.IntReceive("RecStatus", rowHeader, 0);
                if (RecStatus == 1)
                    DocNo += "(ยกเลิก)";
            }
            Exception ex = null;
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save ETAX";
            saveDialog.Filter = "ETAX(*.etax)|*.etax";
            saveDialog.FileName = DocNo;
            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;
            string filepathPDF = saveDialog.FileName.Replace(".etax", ".pdf");
            filepathPDF = UtilityClass.GetUniqueFileName(filepathPDF);
            string filepathXML = saveDialog.FileName.Replace(".etax", ".xml");
            filepathXML = UtilityClass.GetUniqueFileName(filepathXML);


            int TransErr = 0;
            if (TransErr == 0 && this.UseEtax)
                TransErr = this.SignXML_Test(filepathXML, ref ErrMsg);

            if (TransErr == 0)
                TransErr = this.SignPDF(this.DataSetObj
                        , this.ReportObj
                        , this.rowHeader
                        , this.TableNameDetails
                        , this.FIELD_DocNo
                        , this.PrintInsertRow
                        , DateSign
                        , filepathPDF
                        , true
                        , ref ErrMsg
                        , ref ex);

            //if (TransErr == 0)
            //    TransErr += this.UploadFile(this.rowHeader, DateSign, filepathPDF, filepathXML, ref ErrMsg, ref ex);
            if (TransErr == 0)
            {
                MySuccessMessageBox.Show("Test Sign ETAX เสร็จเรียบร้อย");
                try
                {
                    System.Diagnostics.Process.Start(filepathPDF);
                }
                catch { }
                this.LoadETAX();
            }
            else
            {
                if (ex != null)
                {
                    string ExcaptionError = "";
                    if (ex.InnerException != null)
                    {
                        ExcaptionError = $@"InnerException Message : {ex.InnerException.Message}
StackTrace : {ex.StackTrace}";
                    }
                    else
                    {
                        ExcaptionError = ex.StackTrace;
                    }
                    MessageBox.Show(ErrMsg, ExcaptionError);
                }
                else
                    MyMessageBox.Show(ErrMsg);
            }
        }
        private int SignXML_Test(string filepath, ref string ErrMsg)
        {
            int TransErr = 0;
            try
            {
                string DocumentType = "";
                string input = "";
                TransErr = this.GetXMLLayout(ref DocumentType, ref input, ref ErrMsg);
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
        private void SignPDF3A_Click(object sender, EventArgs e)
        {
            string ErrMsg = "";
            DateTime DateSign = DateTime.Now;
            string DocNo = "";
            if (rowHeader.Table.Columns.Contains(FIELD_DocNo))
            {
                DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader).Replace("/", "-");
                int RecStatus = ReceiveValue.IntReceive("RecStatus", rowHeader, 0);
                if (RecStatus == 1)
                    DocNo += "(ยกเลิก)";
            }
            Exception ex = null;
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save PDF/A-3";
            saveDialog.Filter = "PDF/A-3(*.pdf)|*.pdf";
            saveDialog.FileName = DocNo;
            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;
            string filepathPDF = saveDialog.FileName;
            string DocumentType = "";
            string XMLData = "";
            int TransErr = this.GetXMLLayout(ref DocumentType, ref XMLData, ref ErrMsg);

            if (TransErr == 0)
            {
                TransErr = this.GenPDF3A(this.DataSetObj
                           , this.ReportObj
                           , this.rowHeader
                           , this.TableNameDetails
                           , this.FIELD_DocNo
                           , this.PrintInsertRow
                           , DateSign
                           , filepathPDF
                           , XMLData
                           , DocumentType
                           , ref ErrMsg
                           , ref ex);
            }

            if (TransErr == 0)
                TransErr += this.UploadFile(this.rowHeader, DateSign, filepathPDF, "", ref ErrMsg, ref ex);
            if (TransErr == 0)
            {
                string DocumentModuleCode = ReceiveValue.StringReceive("DocumentModuleCode", rowHeader);
                if (DocumentModuleCode == "SITI" && this.CheckEdit_AutoSendMail.Checked)
                {
                    DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader);
                    string CustomerCode = ReceiveValue.StringReceive("CustomerCode", rowHeader);
                    TransErr += this.EtaxMail.AutoSendMail(DocumentModuleCode, DocNo, CustomerCode, filepathPDF, ref ErrMsg, ref ex);
                }
            }
            if (TransErr == 0)
            {
                MySuccessMessageBox.Show("Gen PDF/A-3 เสร็จเรียบร้อย");
                try
                {
                    System.Diagnostics.Process.Start(filepathPDF);
                }
                catch { }
                this.LoadETAX();
            }
            else
            {
                if (ex != null)
                {
                    string ExcaptionError = "";
                    if (ex.InnerException != null)
                    {
                        ExcaptionError = $@"InnerException Message : {ex.InnerException.Message}
StackTrace : {ex.StackTrace}";
                    }
                    else
                    {
                        ExcaptionError = ex.StackTrace;
                    }
                    MessageBox.Show(ErrMsg, ExcaptionError);
                }
                else
                    MyMessageBox.Show(ErrMsg);
            }
        }
        private void TestPDF_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string ErrMsg = "";
            DateTime DateSign = DateTime.Now;
            string DocNo = "";
            if (rowHeader.Table.Columns.Contains(FIELD_DocNo))
            {
                DocNo = ReceiveValue.StringReceive(FIELD_DocNo, rowHeader).Replace("/", "-");
                int RecStatus = ReceiveValue.IntReceive("RecStatus", rowHeader, 0);
                if (RecStatus == 1)
                    DocNo += "(ยกเลิก)";
            }
            Exception ex = null;
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save PDF/A-3";
            saveDialog.Filter = "PDF/A-3(*.pdf)|*.pdf";
            saveDialog.FileName = DocNo;
            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;
            string filepathPDF = saveDialog.FileName;
            string DocumentType = "";
            string XMLData = "";
            int TransErr = this.GetXMLLayout(ref DocumentType, ref XMLData, ref ErrMsg);

            if (TransErr == 0)
            {
                TransErr = this.GenPDF3A(this.DataSetObj
                           , this.ReportObj
                           , this.rowHeader
                           , this.TableNameDetails
                           , this.FIELD_DocNo
                           , this.PrintInsertRow
                           , DateSign
                           , filepathPDF
                           , XMLData
                           , DocumentType
                           , ref ErrMsg
                           , ref ex);
            }
            if (TransErr == 0)
            {
                MySuccessMessageBox.Show("Test Gen PDF/A-3 เสร็จเรียบร้อย");
                try
                {
                    System.Diagnostics.Process.Start(filepathPDF);
                }
                catch { }
            }
            else
            {
                if (ex != null)
                {
                    string ExcaptionError = "";
                    if (ex.InnerException != null)
                    {
                        ExcaptionError = $@"InnerException Message : {ex.InnerException.Message}
StackTrace : {ex.StackTrace}";
                    }
                    else
                    {
                        ExcaptionError = ex.StackTrace;
                    }
                    MessageBox.Show(ErrMsg, ExcaptionError);
                }
                else
                    MyMessageBox.Show(ErrMsg);
            }
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
    }
}
