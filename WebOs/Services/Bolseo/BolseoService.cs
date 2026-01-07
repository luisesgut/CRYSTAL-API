using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebOs.Services.Bolseo
{
    public class BolseoService
    {
        private readonly string server = @"172.16.10.5\SISPRO2023";
        private readonly string database = "SisPro";
        private readonly string user = "general";
        private readonly string password = "Sispro123";

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