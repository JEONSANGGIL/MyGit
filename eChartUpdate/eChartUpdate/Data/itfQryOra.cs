using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace eChartUpdate.Data
{
    public interface itfQryOra : IDisposable
    {
        OracleParameterCollection GetParameters();
        string Qry { get; set; }        
        OracleDataReader GetDataReader();
        OracleDataReader GetDataReader(OracleParameterCollection? p_objPara);
        DataTable GetDataTable();
        DataTable GetDataTable(OracleParameterCollection? p_objPara);
        DataTable GetDataTableBlob(string p_CONNECT_STRING);
        DataTable GetDataTableBlob(OracleParameterCollection? p_objPara, string p_CONNECT_STRING);
        int ExecuteNonQuery();
        int ExecuteNonQuery(OracleParameterCollection p_objPara);
        object ExecuteScalar(OracleParameterCollection p_objPara);
        bool subFindTable(string table);
        bool IsExistTable(string p_strTableName);
        [Obsolete("해당 함수 대신 IsExistColumn 함수를 사용해주세요.", false)]
        bool subFindColumn(string table, string column);
        bool IsExistColumn(string p_strTableName, string p_strColumnName);
        public void BeginTransaction();
        public void CommitTransaction();
        public void RollbackTransaction();        

        string ExecuteScalarQuery();
        string ExecuteScalarQuery(OracleParameterCollection p_objPara);
        bool IsExistObject(string p_strObject);
    }
}
