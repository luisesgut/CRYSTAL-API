using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace WebOs.Controllers
{
    public class ReporteFechaYMaquinaController : ApiController
    {
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports";

        [HttpGet]
        [Route("api/fechaYmaquina")]
        public HttpResponseMessage GenerarPorFechaYMaquina(string fecha, string maquina)
        {
            try
            {
                // Validar parámetros
                if (string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(maquina))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ Los parámetros 'fecha' y 'maquina' son obligatorios.");

                // Verificar existencia del archivo .rpt
                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsExtrusion/ReporteFechaYMaquina.rpt");
                if (!File.Exists(rutaReporte))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"❌ No se encontró el reporte en: {rutaReporte}");

                // Cargar reporte
                ReportDocument reporte = new ReportDocument();
                reporte.Load(rutaReporte);
                reporte.Refresh();

                // Conexión a la BD
                ConnectionInfo conn = new ConnectionInfo
                {
                    ServerName = "172.16.10.113,1433",
                    DatabaseName = "bioflex_sap",
                    UserID = "User_PBI",
                    Password = "PBI*2025",
                    IntegratedSecurity = false
                };

                foreach (Table table in reporte.Database.Tables)
                {
                    TableLogOnInfo logonInfo = table.LogOnInfo;
                    logonInfo.ConnectionInfo = conn;
                    table.ApplyLogOnInfo(logonInfo);
                    table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
                }

                // Establecer parámetros
                reporte.SetParameterValue("fecha", fecha);
                reporte.SetParameterValue("maquina", maquina);

                // Crear nombre único del archivo
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"Reporte_{fecha}_{maquina}_{timestamp}.pdf";

                // Asegurar que la carpeta de destino existe
                if (!Directory.Exists(_storagePath))
                    Directory.CreateDirectory(_storagePath);

                // Ruta de destino del PDF
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar a disco
                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

                // Leer archivo para respuesta HTTP
                Stream pdf = new FileStream(rutaDestino, FileMode.Open, FileAccess.Read);
                pdf.Seek(0, SeekOrigin.Begin);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(pdf)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                {
                    FileName = fileName
                };

                return response;
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToLower();

                if (msg.Contains("logon failed") || msg.Contains("no se pudo conectar"))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                        "❌ No se pudo conectar con la base de datos. Verifica usuario, clave o permisos.");
                }

                if (msg.Contains("missing parameter values") || msg.Contains("parámetro"))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "❌ Faltan parámetros requeridos en el reporte.");
                }

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }
    }
}
