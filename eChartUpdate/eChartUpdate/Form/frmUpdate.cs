// todo : 전상길 20221214 사용않는 using 제거

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
        #region 폼 이벤트 관련
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
            /* kdw 심화학습 제안 
               기존 로깅 기록을 RichText에 표시하는데
               로깅 기록을 파일에 기록할지 RichText에 기록할지 가변적으로 만들려면 ????
               로깅 개체를 외부에서 주입하는 방법을 고민... IOC/DI... 구글링...
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
            
            Logging("테이블 작업 시작합니다.");
            TableSetting();            
            stopwatch.Stop(); 
            ts = stopwatch.Elapsed;
            Logging($"테이블 작업 완료시간 : {ts.Hours} 시간 {ts.Minutes} 분 {ts.Seconds} 초");            

            stopwatch.Reset();
            stopwatch.Start();

            Logging("파일 다운로드를 시작합니다");
            Logging("최신 버전 확인 중입니다.");            
            DownloadFile(url, ftp.target_path);                      
            stopwatch.Stop();
            ts = stopwatch.Elapsed;
            Logging($"파일 다운로드 작업 완료시간 : {ts.Hours} 시간 {ts.Minutes} 분 {ts.Seconds} 초");

            progressBar1.Value = progressBar1.Maximum;
            Logging("E-Chart 업데이트 완료");            

            if (MessageBox.Show(this, "업데이트 완료되었습니다. 종료 하시겠습니까", "E-Chart Update Success", MessageBoxButtons.YesNo) == DialogResult.Yes)                
            {
                this.Close();
            }                                 
        }

        #endregion

        #region FTP 관련
        // FTP 서버 접속 함수
        private FtpWebResponse Connect(String url, string method, Action<FtpWebRequest> action = null)
        {
            // WebRequest 클래스를 이용해 접속하기 때문에 객체를 가져온다. (FtpWebRequest로 변환)
            var request = WebRequest.Create(url) as FtpWebRequest;
            // Binary 형식으로 사용한다.
            request.UseBinary = true;
            // FTP 메소드 설정(아래에 별도 설명)
            request.Method = method;
            // 로그인 인증
            request.Credentials = new NetworkCredential(ftp.user, ftp.pwd);
            // request.GetResponse()함수가 호출되면 실제적으로 접속이 되기 때문에, 그전에 설정할 callback 함수 호출
            if (action != null)
            {
                action(request);
            }
            // 접속해서 WebResponse함수를 가져온다.
            return request.GetResponse() as FtpWebResponse;
        }

        /// <summary>
        /// FTP 파일 다운로드
        /// </summary>
        /// <param name="url"></param>      FTP 경로
        /// <param name="target"></param>   다운받을 경로

        private void DownloadFile(string url, string target)
        {                      
            var list = new List<String>();
            // ftp에 접속해서 파일과 디렉토리 리스트를 가져온다.        
            // kdw using (var ...) {} => using var ...           
            using var res = Connect(url, WebRequestMethods.Ftp.ListDirectory);
            using var stream = res.GetResponseStream();
            using var rd = new StreamReader(stream);
                    
            while (true)
            {
                // binary 결과에서 개행(\r\n)의 구분으로 파일 리스트를 가져온다.
                string buf = rd.ReadLine();
                // null이라면 리스트 검색이 끝난 것이다.
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

                    // kdw 문자열 결합을 일관성 있게 사용하면 좋을 듯
                    //     url + "/" + item => $"{url}..."

                    // 파일 시간 체크
                    using var resTime = Connect($"{url}/{item}", WebRequestMethods.Ftp.GetDateTimestamp);
                    
                    FileInfo fi = new FileInfo($"{target}\\{item}");

                    if (!fi.Exists || fi.LastAccessTime < resTime.LastModified)
                    {
                        // 파일을 다운로드한다.
                        using var resDown = Connect($"{url}/{item}", WebRequestMethods.Ftp.DownloadFile);
                        using var streamDown = resDown.GetResponseStream();
                        // stream을 통해 파일을 작성한다.
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
                    // 그러나 파일이 아닌 디렉토리의 경우는 에러가 발생한다.
                    // 로컬 디렉토리를 만든다.
                    Directory.CreateDirectory(target + "\\" + item);
                    // 디렉토리라면 재귀적 방법으로 다시 파일리스트를 탐색한다.
                    DownloadFile(url + "/" + item, target + "\\" + item);
                }
            }
        }
        #endregion

        #region 함수        

        #region 테이블 관련
        /// <summary>
        /// TABLE_CREATE 테이블에서 복사할 테이블 정보 조회하여 테이블 생성
        /// 개발DB의 데이터를 로컬DB로 INSERT        
        /// </summary>
        private void TableSetting()
        {                      
            //Todo 20221212 전상길 - 테이블 하드코딩 개선
            try
            {
                var objQry = new clsQryOraDb();                
                string tableName;    //테이블명
                string scrpit;       //테이블 스크립트
                objQry.Qry = "SELECT * FROM TABLE_CREATE";

                using var dtList = objQry.GetDataTableBlob(clsQryOraDb.개발DB);                                 
                
                foreach (DataRow dr in dtList.Rows)
                {
                    // kdw dr[] null 오류
                    //String.Concat(dr["TABLENAME"]).Trim()
                    //(dr["SCRIPT"] + "").Trim()

                    Application.DoEvents();

                    tableName = dr["TABLENAME"].ToTrimText();
                    scrpit = dr["SCRIPT"].ToTrimText();                    

                    if (!ExistTable(tableName, clsQryOraDb.로컬DB))
                    {
                        TableCreate(clsQryOraDb.로컬DB, scrpit);
                        TableSelect(tableName, clsQryOraDb.개발DB);
                        TableInsert(clsQryOraDb.로컬DB, dr);
                    }
                    else
                    {
                        TableSelect(tableName, clsQryOraDb.개발DB);
                        Application.DoEvents();
                        TableSelect(tableName, clsQryOraDb.로컬DB);

                        // kdw 수정된 자료 삭제 위험 있음
                        if (dtData.Rows.Count != dtData_dev.Rows.Count)
                        {
                            TableDelete(clsQryOraDb.로컬DB, tableName);
                            TableInsert(clsQryOraDb.로컬DB, dr);
                        }
                    }                    
                }                
            }
            catch
            {                
            }                      
        }

        /// <summary>
        /// 테이블 데이터 조회
        /// </summary>
        /// <param name="p_table"></param>  테이블명
        /// <param name="p_db"></param> 개발DB 인지 로컬DB인지 구분
        /// 
        private void TableSelect(string p_table, string p_db)
        {
            var objQry = new clsQryOraDb(p_db);
            objQry.Qry = $"SELECT * FROM {p_table}";

            if (p_db.Equals(clsQryOraDb.개발DB))
            {
                dtData_dev = objQry.GetDataTable();
            }
            else
            { 
                dtData = objQry.GetDataTable();
            }
        }       

        /// <summary>
        /// 개발DB TABLE_CREATE에 등록된 TABLE SCRIPT를 ';' 단위로 실행
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
        /// \r\n 만 있는 경우 예외처리 하기 위해 추가
        /// </summary>
        /// <param name="p_qry"></param>    쿼리
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
        /// 특정 테이블 값 DELETE
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
        /// 개발DB 데이터를 읽어와서 로컬DB에 INSERT
        /// 개발DB TABLE_CREATE 에서 데이터 읽어 오도록
        /// </summary>
        /// <param name="p_db"></param>
        /// <param name="dr"></param>
        private void TableInsert(string p_db, DataRow dr)
        {
            // kdw 제일 위로 이동 , 불필요한 new 실행 방지를 위해
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
                // kdw Transcation 위치 재고
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
        /// 테이블 존재 유무 확인
        /// </summary>
        /// <param name="p_tableName"></param>  테이블명
        /// <param name="p_db"></param> 개발DB 인지 로컬DB인지 구분
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
        /// tnsnames.ora 파일 찾아서 실행경로에 복사
        /// tape : A  -> 환경변수의 TNS_ADMIN 을 레지스트리에서 구함
        /// tape : B  -> 오라클 설치 경로 ORACLE_HOME 을 레지스트리에서 구함
        /// </summary>
        /// <returns></returns>       
        private bool getTnsNames()
        {
            try
            {                
                var reg = Registry.CurrentUser.OpenSubKey("Environment");
                RegistryKey regTemp;

                var type = "A";

                string local_tnspath;            //TNS_NAMES 현재 경로
                string local_tnspath_file;       //TNS_NAMES 현재 경로 + 파일명                
                
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
                    MessageBox.Show(this, "TNS_NAME 경로를 찾지 못했습니다.");
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
        /// 실행 경로에 log.txt 생성
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

        #region 선언

        private struct FTP
        {
            public string ipAdder;        // ftp ip 주소
            public string port;          // ftp 포트 정보 ( 생략 가능 )
            public string folder;           // 최초 다운로드 FTP 경로        
            public string folder_Update;    // 업데이트 FTP 경로        
            public string user;          // ftp user
            public string pwd;           // ftp pwd
            public string target_path;  //ftp 다운받을 경로                                                             
        }

        FTP ftp;

        private struct LOG
        {
            public string path;     // 로그 폴더
            public string file;     // 로그 파일명
        }

        LOG log;

        // kdw readonly 생성자에서 초기화, 여기 초기화 불필요
        private readonly string tnsnames;    //tnsnames 명칭                

        private DataTable dtData = new();       //로컬DB 데이터 테이블
        private DataTable dtData_dev = new();   //개발DB 데이터 테이블

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