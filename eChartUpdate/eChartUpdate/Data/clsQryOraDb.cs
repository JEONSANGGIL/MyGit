using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace eChartUpdate
{
    public static class clsExtensionOracle
    {
        public static void Set(this OracleParameterCollection p, string name, object val)
        {
            if (p.Contains(name) == true)
            {
                p[name].Value = val;
            }
            else
            {
                p.Add(new OracleParameter(name, val));
            }
        }
    }
}

namespace eChartUpdate.Data
{
    public class clsQryOraDb : itfQryOra, IDisposable
    {
        //private static clsQryOraDb? _instance = null;
        private static Lazy<clsQryOraDb> _instance = new();

        //public static clsQryOraDb Instance
        //{
        //    get { return _instance ?? (_instance = new clsQryOraDb()); }
        //}
        public static clsQryOraDb Instance => _instance.Value;

        //private const string CONNECT_STRING = "Data Source=kti;User ID=kti;Password=kti2kds;";
        public const string 개발DB = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=114.201.95.50)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=kti)));User Id=kti;Password=kti2kds;";
        public const string 로컬DB = "Data Source=kti;User ID=kti;Password=kti2kds;";        
        private OracleConnection m_objConnection = new();        
        private OracleCommand m_objCommand = new();
        private string m_strQry = string.Empty;        

        public string Qry
        {
            get { return m_strQry; }
            set { m_strQry += value; }
        }

        private string QryLog
        {
            get
            {
                var strQry = m_strQry;
                foreach (OracleParameter param in m_objCommand.Parameters)
                {
                    strQry = strQry.Replace($":{param.ParameterName}", $"'{param.Value}'");
                }
                return strQry;
            }
        }
        
        private string QryLogOleDb(System.Data.OleDb.OleDbCommand p_oleDbCommand)
        {
            var strQry = m_strQry;
            foreach (System.Data.OleDb.OleDbParameter param in p_oleDbCommand.Parameters)
            {
                strQry = strQry.Replace($":{param.ParameterName}", $"'{param.Value}'");
            }
            return strQry;
        }


        public clsQryOraDb() : this(개발DB)
        {

        }

        public clsQryOraDb(string p_CONNECT_STRING)
        {
            try
            {                
                // .NET6 로그파일 한글 저장을 위해 설정
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                m_objConnection.ConnectionString = p_CONNECT_STRING;                                
                m_objConnection.Open();                

                m_objCommand.Connection = m_objConnection;
                m_objCommand.BindByName = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "데이터베이스 연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Dispose();
            }
        }       

        public void BeginTransaction()
        {
            m_objCommand.Transaction = m_objConnection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (m_objCommand.Transaction != null) m_objCommand.Transaction.Commit();
        }

        public void RollbackTransaction()
        {
            if (m_objCommand.Transaction != null) m_objCommand.Transaction.Rollback();
        }

        public OracleParameterCollection GetParameters()
        {
            return new OracleCommand().Parameters;
        }

        public OracleDataReader GetDataReader()
        {
            return GetDataReader(null);
        }

        public OracleDataReader GetDataReader(OracleParameterCollection? p_objPara)
        {
#if DEBUG
            var strFuncName = new StackFrame(1, true).GetMethod()?.Name;
            var strClassName = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name;
#endif

            try
            {
                m_objCommand.CommandText = Qry;
                SetParameters(p_objPara);
#if DEBUG
                SaveLogInfo(QryLog, strClassName, strFuncName);
#endif
                return m_objCommand.ExecuteReader();
            }
            catch (Exception ex)
            {
#if DEBUG
                SaveLogInfo(ex.Message, strClassName, strFuncName);
                SaveLogInfo(new StackTrace(ex).ToString(), strClassName, strFuncName);
#endif
                throw;
            }
            finally
            {
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();
            }
        }

        public DataTable GetDataTable()
        {
            return GetDataTable(null);
        }

        public DataTable GetDataTable(OracleParameterCollection? p_objPara)
        {
#if DEBUG
            var strFuncName = new StackFrame(1, true).GetMethod()?.Name;
            var strClassName = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name;
#endif

            try
            {
                m_objCommand.CommandText = Qry;
                SetParameters(p_objPara);
#if DEBUG
                SaveLogInfo(QryLog, strClassName, strFuncName);
#endif
                using var dtList = new DataTable();
                dtList.Load(m_objCommand.ExecuteReader());

                //using (OracleDataAdapter oda = new OracleDataAdapter(m_objCommand)) 
                //{
                //    oda.Fill(dtList);
                //}
                foreach (DataColumn dcCol in dtList.Columns)
                {
                    if (dcCol.ReadOnly) dcCol.ReadOnly = false;
                }
                return dtList;
            }
            catch (Exception ex)
            {
#if DEBUG
                SaveLogInfo(ex.Message, strClassName, strFuncName);
                SaveLogInfo(new StackTrace(ex).ToString(), strClassName, strFuncName);
#endif
                throw;
            }
            finally
            {
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();
            }
        }

        public DataTable GetDataTableBlob(string p_CONNECT_STRING)
        {
            return GetDataTableBlob(null, p_CONNECT_STRING);
        }

        public DataTable GetDataTableBlob(OracleParameterCollection? p_objPara, string p_CONNECT_STRING)
        {
#if DEBUG
            var strFuncName = new StackFrame(1, true).GetMethod()?.Name;
            var strClassName = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name;
#endif

            using var _oleConnection = new System.Data.OleDb.OleDbConnection($"Provider=OraOLEDB.Oracle;{p_CONNECT_STRING}");

            try
            {
                _oleConnection.Open();
                using var _objCommand = new System.Data.OleDb.OleDbCommand(Qry, _oleConnection);

                SetParametersOleDb(p_objPara, _objCommand);
#if DEBUG
                SaveLogInfo(QryLogOleDb(_objCommand), strClassName, strFuncName);
#endif
                using var dtList = new DataTable();
                dtList.Load(_objCommand.ExecuteReader());
                foreach (DataColumn dcCol in dtList.Columns)
                {
                    if (dcCol.ReadOnly) dcCol.ReadOnly = false;
                }
                return dtList;
            }
            catch (Exception ex)
            {
#if DEBUG
                SaveLogInfo(ex.Message, strClassName, strFuncName);
                SaveLogInfo(new StackTrace(ex).ToString(), strClassName, strFuncName);
#endif
                throw;
            }
            finally
            {
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();

                if (_oleConnection.State == ConnectionState.Open)
                {
                    _oleConnection.Close();
                }
            }
        }


        public object ExecuteScalar(OracleParameterCollection p_objPara)
        {
            try
            {
                m_objCommand.CommandText = Qry;
                SetParameters(p_objPara);

                object intRslt = m_objCommand.ExecuteScalar();
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();

                return intRslt;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(m_strQry);
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();

                throw ex;
            }
        }
        public int ExecuteNonQuery()
        {
            return ExecuteNonQuery(null);
        }

        public int ExecuteNonQuery(OracleParameterCollection? p_objPara)
        {
#if DEBUG
            var strFuncName = new StackFrame(1, true).GetMethod()?.Name;
            var strClassName = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name;
#endif
            try
            {
                m_objCommand.CommandText = Qry;
                SetParameters(p_objPara);
#if DEBUG
                SaveLogInfo(QryLog, strClassName, strFuncName);
#endif
                return m_objCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
#if DEBUG
                SaveLogInfo(ex.Message, strClassName, strFuncName);
                SaveLogInfo(new StackTrace(ex).ToString(), strClassName, strFuncName);
#endif
                throw;
            }
            finally
            {
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();
            }
        }
        private void SetParameters(OracleParameterCollection? p_objPara)
        {
            m_objCommand.Parameters.Clear();
            if (p_objPara != null)
            {
                foreach (OracleParameter objPara in p_objPara)
                {
                    //오라클 파라미터 추가 제외
                    if (objPara.ParameterName.StartsWith("@")) continue;

                    if (m_objCommand.Parameters.Contains(objPara.ParameterName))
                    {
                        m_objCommand.Parameters[objPara.ParameterName].Value = objPara.Value;
                    }
                    else
                    {
                        m_objCommand.Parameters.Add(objPara.ParameterName, objPara.Value);
                    }
                }
            }
        }
        private void SetParametersOleDb(OracleParameterCollection? p_objPara, System.Data.OleDb.OleDbCommand p_oleCommand)
        {
            p_oleCommand.Parameters.Clear();
            if (p_objPara != null)
            {
                foreach (OracleParameter objPara in p_objPara)
                {
                    //오라클 파라미터 추가 제외
                    if (objPara.ParameterName.StartsWith("@")) continue;

                    if (p_oleCommand.Parameters.Contains(objPara.ParameterName))
                    {
                        p_oleCommand.Parameters[objPara.ParameterName].Value = objPara.Value;
                    }
                    else
                    {
                        p_oleCommand.Parameters.Add(objPara.ParameterName, objPara.Value);
                    }
                }
            }
        }

        [Obsolete("해당 함수 대신 IsExistTable 함수를 사용해주세요.", false)]
        public bool subFindTable(string table)
        {
            return IsExistTable(table);
        }

        public bool IsExistTable(string p_strTableName)
        {
            m_objCommand.CommandText = $"select count(*) from USER_TAB_COLUMNS where table_name = '{p_strTableName.ToUpper()}'";

            try
            {
                return (decimal)m_objCommand.ExecuteScalar() > 0;
            }
            catch (OracleException)
            {
                return false;
            }
        }

        [Obsolete("해당 함수 대신 IsExistColumn 함수를 사용해주세요.", false)]
        public bool subFindColumn(string table, string column)
        {
            return IsExistColumn(table, column);
        }

        public bool IsExistColumn(string p_strTableName, string p_strColumnName)
        {
            m_objCommand.CommandText = $"select count(*) from USER_TAB_COLUMNS where table_name = '{p_strTableName.ToUpper()}' and column_name = '{p_strColumnName.ToUpper()}'";
            try
            {
                return (decimal)m_objCommand.ExecuteScalar() > 0;
            }
            catch (OracleException)
            {
                return false;
            }
        }

        public bool IsExistObject(string p_strObjectName)
        {
            m_objCommand.CommandText = $"select count(*) from USER_OBJECTS where object_name = '{p_strObjectName.ToUpper()}'";
            try
            {
                return (decimal)m_objCommand.ExecuteScalar() > 0;
            }
            catch (OracleException)
            {
                return false;
            }
        }

        private void SaveLogInfo(string p_strMsg, string? p_strClassName, string? p_strFuncName)
        {
            const int MAX_LOG_SIZE = 102400;
            var strLogPath = $"{Application.StartupPath}log";
            var strLogFile = $"{strLogPath}\\{DateTime.Today.ToString()}.log";

            if (!Directory.Exists(strLogPath))
            {
                Directory.CreateDirectory(strLogPath);
            }

            StreamWriter? sw = null;
            try
            {
                if (!File.Exists(strLogFile) || (new FileInfo(strLogFile).Length > MAX_LOG_SIZE))
                {

                    sw = new StreamWriter(strLogFile, false, System.Text.Encoding.GetEncoding(51949));
                }
                else
                {
                    sw = new StreamWriter(strLogFile, true, System.Text.Encoding.GetEncoding(51949));
                }
                sw.Write($"{DateTime.Now}");
                if (string.IsNullOrEmpty(p_strClassName))
                {
                    sw.WriteLine();
                }
                else
                {
                    sw.WriteLine($" - {p_strClassName}.{p_strFuncName}");
                }
                sw.WriteLine($"{p_strMsg}");
                sw.WriteLine(new string('-', 108));
            }
            catch
            {
            }
            finally
            {
                sw?.Dispose();
            }
        }

        public string ExecuteScalarQuery()
        {
            return ExecuteScalarQuery(null);
        }

        public string ExecuteScalarQuery(OracleParameterCollection? p_objPara)
        {
            var strFuncName = new StackFrame(1, true).GetMethod()?.Name;
            var strClassName = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name;
            try
            {
                m_objCommand.CommandText = Qry;
                SetParameters(p_objPara);
                SaveLogInfo(QryLog, strClassName, strFuncName);
                //return m_objCommand.ExecuteScalar().ToText();
                return m_objCommand.ExecuteScalar().ToString();
            }
            catch (Exception ex)
            {
                SaveLogInfo(ex.Message, strClassName, strFuncName);
                SaveLogInfo(new StackTrace(ex).ToString(), strClassName, strFuncName);
                throw;
            }
            finally
            {
                m_strQry = string.Empty;
                m_objCommand.Parameters.Clear();
            }
        }

        public void Dispose()
        {
            try
            {
                m_objCommand.Dispose();
                if (m_objConnection.State == ConnectionState.Open)
                {
                    m_objConnection.Close();
                }
                m_objConnection.Dispose();
            }
            catch
            {
            }            
        }       
    }
}
