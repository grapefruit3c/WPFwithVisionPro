using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace VisionFramework.Core.Data
{
    /// <summary>
    /// 检测记录 SQLite 数据访问服务。
    /// 数据库默认存放在程序目录 data/records.db。
    /// </summary>
    public class DetectionRecordService
    {
        private readonly string _dbPath;

        public string DbPath => _dbPath;

        public DetectionRecordService(string dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "records.db");
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            Initialize();
        }

        private string ConnectionString => $"Data Source={_dbPath};Version=3;";

        private void Initialize()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS DetectionRecords (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Time TEXT NOT NULL,
                            ProductName TEXT,
                            VppName TEXT,
                            IsOk INTEGER NOT NULL,
                            DurationMs REAL DEFAULT 0,
                            OutputsJson TEXT
                        );";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>保存一条检测记录。</summary>
        public void Add(DetectionRecord record)
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO DetectionRecords (Time, ProductName, VppName, IsOk, DurationMs, OutputsJson)
                            VALUES (@time, @product, @vpp, @isok, @dur, @out);";
                        cmd.Parameters.AddWithValue("@time", record.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        cmd.Parameters.AddWithValue("@product", (object)record.ProductName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@vpp", (object)record.VppName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@isok", record.IsOk ? 1 : 0);
                        cmd.Parameters.AddWithValue("@dur", record.DurationMs);
                        cmd.Parameters.AddWithValue("@out", (object)record.OutputsJson ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        /// <summary>获取最近的检测记录（按时间倒序）。</summary>
        public List<DetectionRecord> GetRecent(int count = 500)
        {
            var list = new List<DetectionRecord>();
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM DetectionRecords ORDER BY Id DESC LIMIT @cnt;";
                        cmd.Parameters.AddWithValue("@cnt", count);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new DetectionRecord
                                {
                                    Id = reader.GetInt32(0),
                                    Time = DateTime.Parse(reader.GetString(1)),
                                    ProductName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    VppName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    IsOk = reader.GetInt32(4) == 1,
                                    DurationMs = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                                    OutputsJson = reader.IsDBNull(6) ? "" : reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        /// <summary>统计 OK/NG 数量。</summary>
        public (int total, int ok, int ng) GetStats()
        {
            int total = 0, ok = 0, ng = 0;
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*), SUM(CASE WHEN IsOk=1 THEN 1 ELSE 0 END), SUM(CASE WHEN IsOk=0 THEN 1 ELSE 0 END) FROM DetectionRecords;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                total = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                ok = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                ng = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            }
                        }
                    }
                }
            }
            catch { }
            return (total, ok, ng);
        }
    }
}
