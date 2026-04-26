namespace GrabAndGo.DataAccess.Core
{
    public class SqlExecutor
    {
        private readonly string _connectionString;

        public SqlExecutor(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Used for GET queries (Returns FOR JSON PATH from SQL Server)
        /// </summary>
        public async Task<T?> ExecuteReaderAsync<T>(string spName, object? parameters = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };

            // Dynamically add parameters if an anonymous object is passed
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    cmd.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
                }
            }

            await conn.OpenAsync();

            // SQL Server 'FOR JSON' splits large JSON across multiple rows, so we stitch it together
            var jsonResult = new StringBuilder();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                jsonResult.Append(reader.GetValue(0).ToString());
            }

            string finalJson = jsonResult.ToString();
            return string.IsNullOrEmpty(finalJson) ? default : JsonSerializer.Deserialize<T>(finalJson);
        }

        /// <summary>
        /// Used for INSERT/UPDATE where the entire C# object is sent as a JSON string
        /// </summary>
        public async Task<T?> ExecuteNonQueryAsync<T>(string spName, object requestBody)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };

            // Serialize the C# object to a JSON string and send to SQL
            cmd.Parameters.AddWithValue("@P_JSON_REQUEST", JsonSerializer.Serialize(requestBody));

            // Prepare the output parameter to catch the SQL JSON response
            var outputParam = new SqlParameter("@P_JSON_RESPONSE", SqlDbType.NVarChar, -1)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputParam);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            var jsonResponse = outputParam.Value?.ToString();
            return string.IsNullOrEmpty(jsonResponse) ? default : JsonSerializer.Deserialize<T>(jsonResponse);
        }

        /// <summary>
        /// Used for simple scalars (like Average Grade) or simple Deletes
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string spName, object? parameters = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };

            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                    cmd.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
            }

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
}