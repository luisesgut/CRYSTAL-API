using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace WebOs.Services
{
    public class CrystalReportService
    {
        private readonly string server = "172.16.10.113,1433";
        private readonly string database = "bioflex_sap";
        private readonly string user = "User_PBI";
        private readonly string password = "PBI*2025";

        public ReportDocument CargarReporte(string rutaReporte)
        {
            if (!File.Exists(rutaReporte))
                throw new FileNotFoundException($"No se encontró el archivo RPT: {rutaReporte}");

            ReportDocument reporte = new ReportDocument();
            reporte.Load(rutaReporte);
            reporte.Refresh();

            ConnectionInfo conn = new ConnectionInfo
            {
                ServerName = server,
                DatabaseName = database,
                UserID = user,
                Password = password,
                IntegratedSecurity = false
            };

            foreach (Table table in reporte.Database.Tables)
            {
                TableLogOnInfo logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo = conn;
                table.ApplyLogOnInfo(logonInfo);
                table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
            }

            return reporte;
        }

        public string ExportarPDF(ReportDocument reporte, string storagePath, string nombreArchivo)
        {
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            string rutaCompleta = Path.Combine(storagePath, nombreArchivo);
            reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaCompleta);
            reporte.Close();
            reporte.Dispose();

            return rutaCompleta;
        }
    }
}
