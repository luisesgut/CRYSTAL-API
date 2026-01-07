
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.IO;

namespace WebOs.Services.Laminado
{
    public class CrystalReportServiceLaminado
    {
        private readonly string server = @"172.16.10.5\SISPRO2023";
        private readonly string database = "SisPro";
        private readonly string user = "general";
        private readonly string password = "Sispro123";

        public ReportDocument CargarReporte(string rutaReporte)
        {
            if (!File.Exists(rutaReporte))
                throw new FileNotFoundException($"No se encontró el archivo RPT: {rutaReporte}");

            var reporte = new ReportDocument();
            reporte.Load(rutaReporte);

            // Logon global de cortesía
            reporte.SetDatabaseLogon(user, password, server, database);

            var conn = new ConnectionInfo
            {
                ServerName = server,
                DatabaseName = database,
                UserID = user,
                Password = password,
                IntegratedSecurity = false
            };

            // 1) Tablas del reporte principal
            ApplyLogonToTables(reporte, conn);

            // 2) Tablas de cada subreporte
            foreach (ReportDocument sub in reporte.Subreports)
                ApplyLogonToTables(sub, conn);

            // 3) Verificar estructura de base de datos
            reporte.VerifyDatabase();

            // 4) Refrescar después del logon/verify
            reporte.Refresh();

            return reporte;
        }

        private static void ApplyLogonToTables(ReportDocument doc, ConnectionInfo conn)
        {
            foreach (Table table in doc.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo = conn;
                table.ApplyLogOnInfo(logonInfo);

                // ⚠️ No reasignar table.Location; respeta esquema/alias/Command de diseño.
                // Si alguna vez necesitas cambiar SOLO la base y conservar esquema.nombre:
                // var parts = table.Location.Split('.');
                // if (parts.Length >= 2)
                //     table.Location = conn.DatabaseName + "." + string.Join(".", parts.Skip(1));
            }
        }

        public string ExportarPDF(ReportDocument reporte, string storagePath, string nombreArchivo)
        {
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            string rutaCompleta = Path.Combine(storagePath, nombreArchivo);
            reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaCompleta);

            // Limpieza
            reporte.Close();
            reporte.Dispose();

            return rutaCompleta;
        }
    }
}
