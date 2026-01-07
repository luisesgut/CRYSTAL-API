using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;

namespace WebOs.Services.Extrusion
{
    public class CrystalReportServiceExtrusion
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

            ConnectionInfo conn = new ConnectionInfo
            {
                ServerName = server,
                DatabaseName = database,
                UserID = user,
                Password = password,
                IntegratedSecurity = false
            };

            // Llamada al método robusto que maneja el reporte principal y los subreportes.
            SetDBLogonForReport(conn, reporte);

            return reporte;
        }

        /// <summary>
        /// Aplica la información de conexión a todas las tablas del reporte principal y sus subreportes.
        /// </summary>
        /// <param name="connectionInfo">La información de conexión a aplicar.</param>
        /// <param name="reportDocument">El ReportDocument a modificar.</param>
        private void SetDBLogonForReport(ConnectionInfo connectionInfo, ReportDocument reportDocument)
        {
            // 1. Aplicar a las tablas del reporte actual (principal o subreporte)
            foreach (Table table in reportDocument.Database.Tables)
            {
                TableLogOnInfo tableLogOnInfo = table.LogOnInfo;
                tableLogOnInfo.ConnectionInfo = connectionInfo;

                try
                {
                    table.ApplyLogOnInfo(tableLogOnInfo);
                    // Crucial: Forzar a Crystal a buscar la tabla en la nueva ubicación de la BD.
                    table.Location = $"{connectionInfo.DatabaseName}.dbo.{table.Name}";
                }
                catch (Exception ex)
                {
                    // Manejo de error si una tabla es un Command/Stored Procedure o falla la reconexión
                    // Puede ser útil para depurar qué tabla falla.
                    Console.WriteLine($"Error al aplicar logon a la tabla {table.Name}: {ex.Message}");
                }
            }

            // 2. Recorrer Subreportes
            foreach (Section section in reportDocument.ReportDefinition.Sections)
            {
                foreach (ReportObject reportObject in section.ReportObjects)
                {
                    if (reportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        SubreportObject subreportObject = (SubreportObject)reportObject;
                        ReportDocument subReport = subreportObject.OpenSubreport(subreportObject.SubreportName);

                        // Llamada recursiva para aplicar la conexión a las tablas del subreporte
                        SetDBLogonForReport(connectionInfo, subReport);
                    }
                }
            }
        }

        // ... (Tu método ExportarPDF se mantiene igual)
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

        public byte[] ExportarPDFBytes(ReportDocument reporte)
        {
            using (var stream = reporte.ExportToStream(ExportFormatType.PortableDocFormat))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                // liberar recursos del reporte
                reporte.Close();
                reporte.Dispose();

                return ms.ToArray();
            }
        }
    }
}