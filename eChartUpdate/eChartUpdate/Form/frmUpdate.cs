// todo : ����� 20221214 ���ʴ� using ����

using eChartUpdate.Data;
using Microsoft.Win32;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace eChartUpdate
{ 
    public partial class frmUpdate : System.Windows.Forms.Form
    {
        #region �� �̺�Ʈ ����
        public frmUpdate(string p_logGubun)
        {
            InitializeComponent();
            Load += frmUpdate_Load;
            Shown += frmUpdate_Shown;                       

            tnsnames = "tnsnames.ora";
            ftp.target_path = @"C:\E-Chart";            
            ftp.ipAdder = "114.201.95.50";
            ftp.port = string.Empty;
            ftp.user = "edb";
            ftp.pwd = "edb";
            ftp.folder = "E-Chart";
            ftp.folder_Update = "Update";
            log.path = @"logging";
            log.file = "log.txt";

            logGubun = p_logGubun;
        }             

        private void frmUpdate_Load(object? sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            ProgressbarInit();
            LogInit();
        }

        private void frmUpdate_Shown(object? sender, EventArgs e)
        {
            /* kdw ��ȭ�н� ���� 
               ���� �α� ����� RichText�� ǥ���ϴµ�
               �α� ����� ���Ͽ� ������� RichText�� ������� ���������� ������� ????
               �α� ��ü�� �ܺο��� �����ϴ� ����� ���... IOC/DI... ���۸�...
            */            

            string url;
            TimeSpan ts;
            Stopwatch stopwatch = new Stopwatch();                             

            if (!Directory.Exists(ftp.target_path))
            {
                Directory.CreateDirectory(ftp.target_path);                
                url = $@"FTP://{ftp.ipAdder}:{ftp.port}//{ftp.folder}";
            }
            else
            {                
                url = $@"FTP://{ftp.ipAdder}:{ftp.port}//{ftp.folder_Update}";
            }                     

            stopwatch.Start();            
            
            if (!getTnsNames()) return;            
            
            Logging("���̺� �۾� �����մϴ�.");
            TableSetting();            
            stopwatch.Stop(); 
            ts = stopwatch.Elapsed;
            Logging($"���̺� �۾� �Ϸ�ð� : {ts.Hours} �ð� {ts.Minutes} �� {ts.Seconds} ��");            

            stopwatch.Reset();
            stopwatch.Start();

            Logging("���� �ٿ�ε带 �����մϴ�");
            Logging("�ֽ� ���� Ȯ�� ���Դϴ�.");            
            DownloadFile(url, ftp.target_path);                      
            stopwatch.Stop();
            ts = stopwatch.Elapsed;
            Logging($"���� �ٿ�ε� �۾� �Ϸ�ð� : {ts.Hours} �ð� {ts.Minutes} �� {ts.Seconds} ��");

            progressBar1.Value = progressBar1.Maximum;
            Logging("E-Chart ������Ʈ �Ϸ�");            

            if (MessageBox.Show(this, "������Ʈ �Ϸ�Ǿ����ϴ�. ���� �Ͻðڽ��ϱ�", "E-Chart Update Success", MessageBoxButtons.YesNo) == DialogResult.Yes)                
            {
                this.Close();
            }                                 
        }

        #endregion

        #region FTP ����
        // FTP ���� ���� �Լ�
        private FtpWebResponse Connect(String url, string method, Action<FtpWebRequest> action = null)
        {
            // WebRequest Ŭ������ �̿��� �����ϱ� ������ ��ü�� �����´�. (FtpWebRequest�� ��ȯ)
            var request = WebRequest.Create(url) as FtpWebRequest;
            // Binary �������� ����Ѵ�.
            request.UseBinary = true;
            // FTP �޼ҵ� ����(�Ʒ��� ���� ����)
            request.Method = method;
            // �α��� ����
            request.Credentials = new NetworkCredential(ftp.user, ftp.pwd);
            // request.GetResponse()�Լ��� ȣ��Ǹ� ���������� ������ �Ǳ� ������, ������ ������ callback �Լ� ȣ��
            if (action != null)
            {
                action(request);
            }
            // �����ؼ� WebResponse�Լ��� �����´�.
            return request.GetResponse() as FtpWebResponse;
        }

        /// <summary>
        /// FTP ���� �ٿ�ε�
        /// </summary>
        /// <param name="url"></param>      FTP ���
        /// <param name="target"></param>   �ٿ���� ���

        private void DownloadFile(string url, string target)
        {                      
            var list = new List<String>();
            // ftp�� �����ؼ� ���ϰ� ���丮 ����Ʈ�� �����´�.        
            // kdw using (var ...) {} => using var ...           
            using var res = Connect(url, WebRequestMethods.Ftp.ListDirectory);
            using var stream = res.GetResponseStream();
            using var rd = new StreamReader(stream);
                    
            while (true)
            {
                // binary ������� ����(\r\n)�� �������� ���� ����Ʈ�� �����´�.
                string buf = rd.ReadLine();
                // null�̶�� ����Ʈ �˻��� ���� ���̴�.
                if (string.IsNullOrWhiteSpace(buf))
                {
                    break;
                }

                list.Add(buf);
            }                                                

            progressBar1.Maximum += list.Count;

            foreach (var item in list)
            {
                try
                {
                    Application.DoEvents();

                    // kdw ���ڿ� ������ �ϰ��� �ְ� ����ϸ� ���� ��
                    //     url + "/" + item => $"{url}..."

                    // ���� �ð� üũ
                    using var resTime = Connect($"{url}/{item}", WebRequestMethods.Ftp.GetDateTimestamp);
                    
                    FileInfo fi = new FileInfo($"{target}\\{item}");

                    if (!fi.Exists || fi.LastAccessTime < resTime.LastModified)
                    {
                        // ������ �ٿ�ε��Ѵ�.
                        using var resDown = Connect($"{url}/{item}", WebRequestMethods.Ftp.DownloadFile);
                        using var streamDown = resDown.GetResponseStream();
                        // stream�� ���� ������ �ۼ��Ѵ�.
                        using var fs = System.IO.File.Create($"{target}\\{item}");
                        streamDown.CopyTo(fs);
                        Logging(item);
                        this.progressBar1.Value += 1;                                                                        
                    }
                    else
                    {
                        this.progressBar1.Value += 1;
                    }                    
                }
                catch (WebException)
                {
                    // �׷��� ������ �ƴ� ���丮�� ���� ������ �߻��Ѵ�.
                    // ���� ���丮�� �����.
                    Directory.CreateDirectory(target + "\\" + item);
                    // ���丮��� ����� ������� �ٽ� ���ϸ���Ʈ�� Ž���Ѵ�.
                    DownloadFile(url + "/" + item, target + "\\" + item);
                }
            }
        }
        #endregion

        #region �Լ�        

        #region ���̺� ����
        /// <summary>
        /// TABLE_CREATE ���̺��� ������ ���̺� ���� ��ȸ�Ͽ� ���̺� ����
        /// ����DB�� �����͸� ����DB�� INSERT        
        /// </summary>
        private void TableSetting()
        {                      
            //Todo 20221212 ����� - ���̺� �ϵ��ڵ� ����
            try
            {
                var objQry = new clsQryOraDb();                
                string tableName;    //���̺��
                string scrpit;       //���̺� ��ũ��Ʈ
                objQry.Qry = "SELECT * FROM TABLE_CREATE";

                using var dtList = objQry.GetDataTableBlob(clsQryOraDb.����DB);                                 
                
                foreach (DataRow dr in dtList.Rows)
                {
                    // kdw dr[] null ����
                    //String.Concat(dr["TABLENAME"]).Trim()
                    //(dr["SCRIPT"] + "").Trim()

                    Application.DoEvents();

                    tableName = dr["TABLENAME"].ToTrimText();
                    scrpit = dr["SCRIPT"].ToTrimText();                    

                    if (!ExistTable(tableName, clsQryOraDb.����DB))
                    {
                        TableCreate(clsQryOraDb.����DB, scrpit);
                        TableSelect(tableName, clsQryOraDb.����DB);
                        TableInsert(clsQryOraDb.����DB, dr);
                    }
                    else
                    {
                        TableSelect(tableName, clsQryOraDb.����DB);
                        Application.DoEvents();
                        TableSelect(tableName, clsQryOraDb.����DB);

                        // kdw ������ �ڷ� ���� ���� ����
                        if (dtData.Rows.Count != dtData_dev.Rows.Count)
                        {
                            TableDelete(clsQryOraDb.����DB, tableName);
                            TableInsert(clsQryOraDb.����DB, dr);
                        }
                    }                    
                }                
            }
            catch
            {                
            }                      
        }

        /// <summary>
        /// ���̺� ������ ��ȸ
        /// </summary>
        /// <param name="p_table"></param>  ���̺��
        /// <param name="p_db"></param> ����DB ���� ����DB���� ����
        /// 
        private void TableSelect(string p_table, string p_db)
        {
            var objQry = new clsQryOraDb(p_db);
            objQry.Qry = $"SELECT * FROM {p_table}";

            if (p_db.Equals(clsQryOraDb.����DB))
            {
                dtData_dev = objQry.GetDataTable();
            }
            else
            { 
                dtData = objQry.GetDataTable();
            }
        }       

        /// <summary>
        /// ����DB TABLE_CREATE�� ��ϵ� TABLE SCRIPT�� ';' ������ ����
        /// </summary>
        /// <param name="p_db"></param>
        /// <param name="p_script"></param>
        private void TableCreate(string p_db, string p_script)
        {
            var objQry = new clsQryOraDb(p_db);

            string[] arrScript = p_script.Split(";");

            foreach (string strQry in arrScript)
            {
                if (Script_Validation(strQry))
                {
                    objQry.Qry = strQry;
                    objQry.ExecuteNonQuery();
                }                
            }
        }

        /// <summary>
        /// \r\n �� �ִ� ��� ����ó�� �ϱ� ���� �߰�
        /// </summary>
        /// <param name="p_qry"></param>    ����
        /// <returns></returns>
        private bool Script_Validation(string p_qry)
        {
            var result = true;

            string strQry = p_qry.Replace("\r", string.Empty).Replace("\n", string.Empty);

            if (strQry.Equals(string.Empty))
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Ư�� ���̺� �� DELETE
        /// </summary>
        /// <param name="p_db"></param>
        /// <param name="p_table"></param>
        private void TableDelete(string p_db, string p_table)
        {
            var objQry = new clsQryOraDb(p_db);

            try
            {
                objQry.Qry = $"DELETE FROM {p_table}";
                objQry.BeginTransaction();
                objQry.ExecuteNonQuery();
                objQry.CommitTransaction();
            }
            catch
            {
                objQry.RollbackTransaction();
            }            
        }       

        /// <summary>
        /// ����DB �����͸� �о�ͼ� ����DB�� INSERT
        /// ����DB TABLE_CREATE ���� ������ �о� ������
        /// </summary>
        /// <param name="p_db"></param>
        /// <param name="dr"></param>
        private void TableInsert(string p_db, DataRow dr)
        {
            // kdw ���� ���� �̵� , ���ʿ��� new ���� ������ ����
            if (dtData_dev.Rows.Count == 0) return;

            var objQry = new clsQryOraDb(p_db);
            var objPara = objQry.GetParameters();            
            var sb = new StringBuilder();
            var sbCol = new StringBuilder();
            var sbPara = new StringBuilder();
            var tableName = dr["TABLENAME"].ToTrimText();            
            string[] arrCol = dr["COL"].ToTrimText().Split(",");            

            try
            {                
                // kdw Transcation ��ġ ���
                objQry.BeginTransaction();

                progressBar1.Maximum += dtData_dev.Rows.Count;

                foreach (DataRow dr_dev in dtData_dev.Rows)
                {
                    sb.Clear();
                    sbCol.Clear();
                    sbPara.Clear();
                    objPara.Clear();

                    Application.DoEvents();

                    foreach (var Col in arrCol.Select((value, index) => (value, index)))
                    {
                        objPara.Set(Col.value, dr_dev[Col.value]);

                        if (Col.index == arrCol.Length - 1)
                        {
                            sbCol.AppendColumnEnd(Col.value);
                            sbPara.AppendColumnParaEnd(Col.value);
                        }
                        else
                        {
                            sbCol.AppendColumn(Col.value);
                            sbPara.AppendColumnPara(Col.value);
                        }
                    }

                    sb.Append($"Insert into {tableName}(");
                    sb.Append(sbCol.ToString());
                    sb.Append("Values(");
                    sb.Append(sbPara.ToString());

                    objQry.Qry = sb.ToString();

                    objQry.ExecuteNonQuery(objPara);

                    this.progressBar1.Value += 1;
                }

                objQry.CommitTransaction();
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message);
                objQry.RollbackTransaction();
            }            
        }

        /// <summary>
        /// ���̺� ���� ���� Ȯ��
        /// </summary>
        /// <param name="p_tableName"></param>  ���̺��
        /// <param name="p_db"></param> ����DB ���� ����DB���� ����
        /// <returns></returns>        
        private bool ExistTable(string p_tableName, string p_db)
        {                   
            return new clsQryOraDb(p_db).IsExistTable(p_tableName);                        
        }

        #endregion       

        private void ProgressbarInit()
        {
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
        }

        /// <summary>
        /// tnsnames.ora ���� ã�Ƽ� �����ο� ����
        /// tape : A  -> ȯ�溯���� TNS_ADMIN �� ������Ʈ������ ����
        /// tape : B  -> ����Ŭ ��ġ ��� ORACLE_HOME �� ������Ʈ������ ����
        /// </summary>
        /// <returns></returns>       
        private bool getTnsNames()
        {
            try
            {                
                var reg = Registry.CurrentUser.OpenSubKey("Environment");
                RegistryKey regTemp;

                var type = "A";

                string local_tnspath;            //TNS_NAMES ���� ���
                string local_tnspath_file;       //TNS_NAMES ���� ��� + ���ϸ�                
                
                var tns = reg.GetValue("TNS_ADMIN");                                    

                if (tns == null)
                {
                    reg = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("ORACLE");

                    foreach (var subKeyName in reg.GetSubKeyNames())
                    {
                        regTemp = reg.OpenSubKey(subKeyName);

                        foreach (string valuename in regTemp.GetValueNames())
                        {
                            if (valuename.Equals("ORACLE_HOME"))
                            {
                                if (tns == null) tns = regTemp.GetValue(valuename);
                                break;
                            }
                        }
                    }
                       
                    type = "B";
                }

                if (tns == null)
                {
                    MessageBox.Show(this, "TNS_NAME ��θ� ã�� ���߽��ϴ�.");
                    return false;
                }

                local_tnspath = (type.ToTrimText() == "A") ? tns.ToTrimText() : $"{tns}\\network\\ADMIN";                    
                local_tnspath_file = System.IO.Path.Combine(local_tnspath, tnsnames);                    
                                        
                System.IO.File.Copy(local_tnspath_file, @"" + tnsnames, true);
                System.IO.File.Copy(local_tnspath_file, @$"{ftp.target_path}\\{tnsnames}", true);

                return true;
                
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message);
                return false;
            }      
        }

        /// <summary>
        /// ���� ��ο� log.txt ����
        /// </summary>
        private void LogInit()
        {            
            if (logGubun == "A")
            {
                string path = $"{log.path}\\{log.file}";

                if (!Directory.Exists(log.path))
                {
                    Directory.CreateDirectory(log.path);
                }

                if (!File.Exists(path))
                { 
                    using var files = File.Create(path);
                }
                
                DateTime dateTime = DateTime.Now;

                LogTxtWrite($"\r\n{dateTime.ToString()}\r\n");

                this.Height = 127;
                panel1.Height = 85;
                txtLog.Visible = false;                
                progressBar1.Location = new Point(progressBar1.Location.X, progressBar1.Location.Y - 89);
                lblTxt.Visible = true;                
            }
        }
 
        private void Logging(string p_cmt)
        {
            switch (logGubun)
            {
                case "A":
                    LogTxtWrite(p_cmt);
                    break;
                default:
                    txtLog.AppendTextNewLine(p_cmt);
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                    break;
            }

            lblTxt.Text = p_cmt;
        }

        private void LogTxtWrite(string p_cmt)
        {
            StreamWriter sw = File.AppendText($"{log.path}\\{log.file}");
            sw.WriteLine(p_cmt);
            sw.Close();
        }      
        #endregion        

        #region ����

        private struct FTP
        {
            public string ipAdder;        // ftp ip �ּ�
            public string port;          // ftp ��Ʈ ���� ( ���� ���� )
            public string folder;           // ���� �ٿ�ε� FTP ���        
            public string folder_Update;    // ������Ʈ FTP ���        
            public string user;          // ftp user
            public string pwd;           // ftp pwd
            public string target_path;  //ftp �ٿ���� ���                                                             
        }

        FTP ftp;

        private struct LOG
        {
            public string path;     // �α� ����
            public string file;     // �α� ���ϸ�
        }

        LOG log;

        // kdw readonly �����ڿ��� �ʱ�ȭ, ���� �ʱ�ȭ ���ʿ�
        private readonly string tnsnames;    //tnsnames ��Ī                

        private DataTable dtData = new();       //����DB ������ ���̺�
        private DataTable dtData_dev = new();   //����DB ������ ���̺�

        private readonly string logGubun;
        #endregion
    }

    public static class clsStringBuilderExtension
    {
        public static StringBuilder AppendColumn(this StringBuilder sb, string s)
        {
            return sb.Append($"{s},"); //
        }
        public static StringBuilder AppendColumnEnd(this StringBuilder sb, string s)
        {
            return sb.Append($"{s})"); //
        }
        public static StringBuilder AppendColumnPara(this StringBuilder sb, string s)
        {
            return sb.Append($":{s},");
        }
        public static StringBuilder AppendColumnParaEnd(this StringBuilder sb, string s)
        {
            return sb.Append($":{s})");
        }
    }

    public static class clsTextBoxExtension
    {
        public static System.Windows.Forms.TextBox AppendTextNewLine(this System.Windows.Forms.TextBox txt, string s)
        {
            txt.Text = string.Concat(txt.Text, $"{s}\r\n");
            txt.Refresh();
            return txt;            
        }
    }   
}